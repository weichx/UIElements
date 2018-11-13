using JetBrains.Annotations;
using UIForia.Systems;
using UnityEngine;

namespace Tests.Mocks {

    public class MockInputSystem : InputSystem {

        public MockInputSystem(ILayoutSystem layoutSystem, IStyleSystem styleSystem)
            : base(layoutSystem, styleSystem) { }

        public void SetMouseState(MouseState mouseState) {
            m_MouseState = mouseState;
        }

        // for the debugger, rider struggles w/ partial classes
        [UsedImplicitly]
        public override void OnUpdate() {
            base.OnUpdate();
        }
        
        protected override MouseState GetMouseState() {
            return m_MouseState;
        }

        public void SetMousePosition(Vector2 position) {
            m_MouseState.previousMousePosition = m_MouseState.mousePosition;
            m_MouseState.mousePosition = position;
        }

        public void MouseDragMove(Vector2 position) {
            m_MouseState.isLeftMouseDown = true;
            m_MouseState.isLeftMouseDownThisFrame = false;
            m_MouseState.isLeftMouseUpThisFrame = false;
            m_MouseState.previousMousePosition = m_MouseState.mousePosition;
            m_MouseState.mousePosition = position;
        }
        
        public void MouseDown(Vector2 position) {
            m_MouseState.isLeftMouseDown = true;
            m_MouseState.isLeftMouseDownThisFrame = true;
            m_MouseState.isLeftMouseUpThisFrame = false;
            m_MouseState.previousMousePosition = m_MouseState.mousePosition;
            m_MouseState.mousePosition = position;
            m_MouseState.mouseDownPosition = position;
        }

        public void MouseUp() {
            m_MouseState.isLeftMouseDown = false;
            m_MouseState.isLeftMouseDownThisFrame = false;
            m_MouseState.isLeftMouseUpThisFrame = true;
            m_MouseState.mouseDownPosition = new Vector2(-1, -1);
        }

        public void ClearClickState() {
            m_MouseState.isLeftMouseDown = false;
            m_MouseState.isLeftMouseDownThisFrame = false;
            m_MouseState.isLeftMouseUpThisFrame = false;
            m_MouseState.mouseDownPosition = new Vector2(-1, -1);
        }

        public void MouseMove(Vector2 position) {
            
        }
    }
}