using System.Collections.Generic;
using UIForia.Parsing.StyleParser;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIForia.Editor {

    public static class RefreshTemplates {

        [MenuItem("UI/Refresh UI Templates %g")]
        public static void Refresh() {
            StyleParser.Reset();
            TemplateParser.Reset();
            ResourceManager.Reset();
            ParsedTemplate.Reset(); // todo -- roll all parsers into one place
            
            Application.Game.Refresh();
            
        }

        public static List<T> FindObjectOfType<T>(bool inactive = true) where T : Object {
            List<T> retn = new List<T>();

            if (!inactive) {
                Object[] items = Object.FindObjectsOfType(typeof(T));
                for (int i = 0; i < items.Length; i++) {
                    retn.Add((T) items[i]);
                }

                return retn;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects()) {
                    var result = root.GetComponentInChildren<T>(true);
                    if (result) {
                        retn.Add(result);
                    }
                }
            }

            return retn;
        }

    }

}