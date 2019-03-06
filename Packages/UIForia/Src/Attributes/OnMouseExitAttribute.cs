using System;
using UIForia.UIInput;

namespace UIForia.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public class OnMouseExitAttribute : MouseEventHandlerAttribute {

        public OnMouseExitAttribute(KeyboardModifiers modifiers = KeyboardModifiers.None, EventPhase phase = EventPhase.Bubble)
            : base(modifiers, InputEventType.MouseExit, phase) { }

        public OnMouseExitAttribute(EventPhase phase)
            : base(KeyboardModifiers.None, InputEventType.MouseExit, phase) { }

    }
}