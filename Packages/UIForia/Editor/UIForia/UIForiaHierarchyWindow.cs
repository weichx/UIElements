using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UIForia.Elements;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UIForia.Editor {

    public class UIForiaHierarchyWindow : EditorWindow {

        public static readonly List<int> EmptyList = new List<int>();
        public const string k_InspectedAppKey = "UIForia.Inspector.ApplicationName";

        public TreeViewState state;
        public HierarchyView treeView;
        private bool needsReload;
        private string inspectedAppId;
        private bool firstLoad;

        private void OnInspectorUpdate() {
            Repaint();
        }

        private static MethodInfo s_GameWindowSizeMethod;

        public static int s_SelectedElementId;
        public static Application s_SelectedApplication;

        public void OnEnable() {
            firstLoad = true;
            state = new TreeViewState();
            autoRepaintOnSceneChange = true;
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
        }

        private void OnElementSelectionChanged(UIElement element) {
            if (element != null) {
                s_SelectedElementId = element.id;
            }
            else {
                s_SelectedElementId = -1;
            }
        }

        private void Refresh(UIElement element) {
            needsReload = true;
        }
        
        private void Refresh(UIView view) {
            needsReload = true;
        }

        public void OnRefresh() {
            s_SelectedElementId = -1;
            treeView?.Destroy();

            Application app = Application.Find(inspectedAppId);

            if (app == null) return;

            treeView = new HierarchyView(app.GetViews(), state);
            treeView.onSelectionChanged += OnElementSelectionChanged;
//            treeView.view = app.GetView(0);
        }

        private void Update() {
            if (!EditorApplication.isPlaying) {
                return;
            }

            if (treeView != null && treeView.selectMode && s_SelectedApplication?.InputSystem.DebugElementsThisFrame.Count > 0) {
                if (s_SelectedApplication.InputSystem.DebugMouseUpThisFrame) {
                    treeView.selectMode = false;
                }
                else {
                    s_SelectedElementId = s_SelectedApplication.InputSystem.DebugElementsThisFrame[s_SelectedApplication.InputSystem.DebugElementsThisFrame.Count - 1].id;
                    IList<int> selectedIds = new List<int>(s_SelectedApplication.InputSystem.DebugElementsThisFrame.Count);

                    int maxT = 0;
                    int selectIdx = 0;
                    for (int i = s_SelectedApplication.InputSystem.DebugElementsThisFrame.Count - 1; i >= 0; i--) {
                        if (s_SelectedApplication.InputSystem.DebugElementsThisFrame[i].depthTraversalIndex > maxT) {
                            maxT = s_SelectedApplication.InputSystem.DebugElementsThisFrame[i].depthTraversalIndex;
                            selectIdx = i;
                        }
                        selectedIds.Add(s_SelectedApplication.InputSystem.DebugElementsThisFrame[i].id);
                    }

                    s_SelectedElementId = s_SelectedApplication.InputSystem.DebugElementsThisFrame[selectIdx].id;
                    treeView.SetSelection(selectedIds);
                    if (selectedIds.Count > 0) {
                        treeView.FrameItem(selectedIds[selectIdx]);
                    }
                }
            }

            Repaint();
        }

        private void SetApplication(string appId) {

            Application oldApp = Application.Find(inspectedAppId);

            if (oldApp != null) {
                oldApp.onElementCreated -= Refresh;
                oldApp.onElementDestroyed -= Refresh;
                oldApp.onViewAdded -= Refresh;
                oldApp.onElementDisabled -= Refresh;
                oldApp.onElementEnabled -= Refresh;
                oldApp.onRefresh -= OnRefresh;
            }

            treeView?.Destroy();

            inspectedAppId = appId;
            EditorPrefs.SetString(k_InspectedAppKey, appId);

            Application app = Application.Find(appId);

            if (app != null) {
                needsReload = true;

                treeView = new HierarchyView(app.GetViews(), state);
                treeView.onSelectionChanged += OnElementSelectionChanged;

                app.onElementCreated += Refresh;
                app.onElementDestroyed += Refresh;
                app.onViewAdded += Refresh;
                app.onElementDisabled += Refresh;
                app.onElementEnabled += Refresh;
                app.onRefresh += OnRefresh;
            }

            s_SelectedApplication = app;
            s_SelectedElementId = -1;
        }

        public void OnGUI() {
            if (!EditorApplication.isPlaying) {
                EditorGUILayout.LabelField("Enter play mode to inspect a UIForia Application");
                return;
            }

            EditorGUILayout.BeginVertical();
            string[] names = new string[Application.Applications.Count + 1];
            names[0] = "None";

            int oldIdx = 0;

            for (int i = 1; i < names.Length; i++) {
                names[i] = Application.Applications[i - 1].id;
                if (names[i] == inspectedAppId) {
                    oldIdx = i;
                }
            }

            int idx = EditorGUILayout.Popup(new GUIContent("Application"), oldIdx, names);
            if (firstLoad || idx != oldIdx) {
                SetApplication(names[idx]);
                if (firstLoad) {
                    s_SelectedApplication = Application.Find(names[idx]);
                    s_SelectedElementId = -1;
                    firstLoad = false;
                }
            }

            if (s_SelectedApplication == null) {
                treeView?.Destroy();
                treeView = null;
            }
            
            if (treeView == null) {
                EditorGUILayout.EndVertical();
                return;
            }

            treeView.showChildrenAndId = EditorGUILayout.Toggle("Show Meta Data", treeView.showChildrenAndId);
            treeView.selectMode = EditorGUILayout.Toggle("Activate Select Mode", treeView.selectMode);
            
            bool wasShowingDisabled = treeView.showDisabled;
            treeView.showDisabled = EditorGUILayout.Toggle("Show Disabled", treeView.showDisabled);
            treeView.showLayoutStats = EditorGUILayout.Toggle("Show Layout Stats", treeView.showLayoutStats);
            
            if (treeView.showDisabled != wasShowingDisabled) {
                needsReload = true;
            }
            if (needsReload) {
                needsReload = false;
                treeView.views = s_SelectedApplication.GetViews();
                treeView.Reload();
                treeView.ExpandAll();
            }

            needsReload = treeView.RunGUI();

            EditorGUILayout.EndVertical();
        }

    }

}