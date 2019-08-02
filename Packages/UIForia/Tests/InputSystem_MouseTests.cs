using System.Collections.Generic;
using NUnit.Framework;
using Tests.Mocks;
using UIForia.Attributes;
using UIForia.Elements;
using UIForia.Systems.Input;
using UIForia.UIInput;
using UnityEngine;

[TestFixture]
public class InputSystemTests {

    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Style>
                style container {
                    LayoutType = Flex;
                    FlexLayoutDirection = Row;
                    PreferredSize = 300px, 100px;
                }
        
                style cell {
                    PreferredSize = 100px;
                }
            </Style>
            <Contents style='container'>
                <Group onMouseDown='{HandleClickedChild(0)}' style='cell' />
                <Group onMouseDown='{HandleClickedChild(1)}' style='cell' />
                <Group onMouseDown='{HandleClickedChild(2)}' style='cell' />
            </Contents>
        </UITemplate>

    ")]
    public class InputSystemTestThing : UIElement {

        public int clickedChildIndex = -1;
        public bool wasMouseDown;

        [OnMouseDown]
        public void OnAnyMouseDown(MouseInputEvent evt) {
            wasMouseDown = true;
        }

        public void HandleClickedChild(int index) {
            clickedChildIndex = index;
        }

    }

    [Template(TemplateType.String, @"
    
        <UITemplate>
            <Style>
                style grid-container {
                    LayoutType = Flex;
                    FlexLayoutDirection = Row;
                    PreferredSize = 300px, 100px;
                }
        
                style cell {
                    PreferredSize = 100px;
                }
            </Style>
            <Contents style='grid-container'>
                <Group onMouseUp='{HandleMouseUpChild($event, 0)}'
                       onMouseDown='{HandleMouseDownChild($event, 0)}' 
                       onMouseEnter='{HandleMouseEnterChild($event, 0)}'
                       onMouseExit='{HandleMouseExitChild($event, 0)}'
                       onMouseMove='{HandleMouseMoveChild($event, 0)}'
                       onMouseHover='{HandleMouseHoverChild($event, 0)}'
                       style='cell' 
                />
                <Group onMouseDown.capture='{HandleMouseDownChild($event, 1)}'
                       onMouseUp.capture='{HandleMouseUpChild($event, 1)}'
                       onMouseEnter.capture='{HandleMouseEnterChild($event, 1)}'
                       onMouseMove='{HandleMouseMoveChild($event, 1)}'
                       style='cell' 
                />
                <Group onMouseDown='{HandleMouseDownChild($event, 2)}' 
                       onMouseUp='{HandleMouseUpChild($event, 2)}'
                       onMouseEnter='{HandleMouseEnterChild($event, 2)}'
                       onMouseExit='{HandleMouseExitChild($event, 2)}'
                       style='cell' 
                />
            </Contents>
        </UITemplate>

    ")]
    public class InputSystemTestThing2 : UIElement {

        public int clickedChildIndex = -1;
        public bool wasMouseDown;
        public bool shouldStopPropagation;
        public List<string> clickList = new List<string>();
        public bool ignoreEnter = true;
        public bool ignoreExit = true;
        public bool ignoreMove = true;
        public bool ignoreHover = true;

        [OnMouseExit]
        public void MouseExit(MouseInputEvent evt) {
            if (ignoreExit) return;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("exit:root");
            wasMouseDown = true;
        }

        [OnMouseMove]
        public void OnMouseMove(MouseInputEvent evt) {
            if (ignoreMove) return;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("move:root");
        }
        
        [OnMouseHover]
        public void OnMouseHover(MouseInputEvent evt) {
            if (ignoreHover) return;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("hover:root");
        }

        [OnMouseUp]
        public void OnAnyMouseUp(MouseInputEvent evt) {
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("up:root");
            wasMouseDown = true;
        }

        [OnMouseDown]
        public void OnAnyMouseDown(MouseInputEvent evt) {
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("down:root");
            wasMouseDown = true;
        }

        [OnMouseEnter]
        public void OnEnter(MouseInputEvent evt) {
            if (ignoreEnter) return;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("enter:root");
            wasMouseDown = true;
        }

        public void HandleMouseEnterChild(MouseInputEvent evt, int index) {
            if (ignoreEnter) return;
            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("enter:child" + index);
        }

        public void HandleMouseExitChild(MouseInputEvent evt, int index) {
            if (ignoreExit) return;

            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("exit:child" + index);
        }

        public void HandleMouseMoveChild(MouseInputEvent evt, int index) {
            if (ignoreMove) return;

            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("move:child" + index);
        }
        
        public void HandleMouseHoverChild(MouseInputEvent evt, int index) {
            if (ignoreHover) return;

            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("hover:child" + index);
        }

        public void HandleMouseDownChild(MouseInputEvent evt, int index) {
            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("down:child" + index);
        }

        public void HandleMouseUpChild(MouseInputEvent evt, int index) {
            clickedChildIndex = index;
            if (shouldStopPropagation) {
                evt.StopPropagation();
            }

            clickList.Add("up:child" + index);
        }

    }

    [Test]
    public void MouseDown_WithPropagation() {
        MockApplication application = new MockApplication(typeof(InputSystemTestThing));
        application.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing root = (InputSystemTestThing) application.RootElement.GetChild(0);
        application.Update();

        application.InputSystem.MouseDown(new Vector2(20, 10));
        application.Update();
        
        
        Assert.AreEqual(0, root.clickedChildIndex);
        Assert.IsTrue(root.wasMouseDown);

        application.InputSystem.ClearClickState();
        application.Update();
        
        application.InputSystem.MouseDown(new Vector2(120, 10));
        application.Update();
        Assert.AreEqual(1, root.clickedChildIndex);

        application.InputSystem.ClearClickState();
        application.Update();
        
        application.InputSystem.MouseDown(new Vector2(220, 10));
        application.Update();
        Assert.AreEqual(2, root.clickedChildIndex);
    }

    [Test]
    public void MouseDown_StopPropagation() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        testView.InputSystem.MouseDown(new Vector2(20, 10));
        
        root.shouldStopPropagation = true;
        testView.Update();

        Assert.AreEqual(0, root.clickedChildIndex);
        Assert.IsFalse(root.wasMouseDown);

        testView.InputSystem.MouseUp();
        testView.Update();
        
        root.shouldStopPropagation = false;
        testView.InputSystem.MouseDown(new Vector2(120, 10));
        testView.Update();

        Assert.AreEqual(1, root.clickedChildIndex);
        Assert.IsTrue(root.wasMouseDown);
    }

    [Test]
    public void MouseDown_StopPropagationInBubble() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        testView.InputSystem.MouseDown( new Vector2(120, 10));
        root.shouldStopPropagation = true;
        testView.Update();

        Assert.AreEqual(-1, root.clickedChildIndex);
        Assert.IsTrue(root.wasMouseDown);

        testView.InputSystem.ClearClickState();
        testView.Update();

        root.wasMouseDown = false;

        root.shouldStopPropagation = false;
        testView.InputSystem.MouseDown( new Vector2(120, 10));
        testView.Update();

        Assert.AreEqual(1, root.clickedChildIndex);
        Assert.IsTrue(root.wasMouseDown);
    }

    [Test]
    public void MouseDown_BubbleThenCapture() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        testView.Update();
        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isDown = true;
        mouseState.leftMouseButtonState.isDownThisFrame = true;

        mouseState.mousePosition = new Vector2(120, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        Assert.AreEqual(new[] {"down:root", "down:child1"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseDown_OutOfBounds() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isDown = true;
        mouseState.leftMouseButtonState.isDownThisFrame = true;

        mouseState.mousePosition = new Vector2(1200, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        Assert.AreEqual(new string[0], root.clickList.ToArray());
    }

    [Test]
    public void MouseUp_FiresAndPropagates() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isUpThisFrame = true;
        mouseState.mousePosition = new Vector2(220, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        Assert.AreEqual(new[] {"up:child2", "up:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseUp_StopPropagation() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isUpThisFrame = true;
        mouseState.mousePosition = new Vector2(220, 10);
        testView.InputSystem.SetMouseState(mouseState);
        root.shouldStopPropagation = true;
        testView.Update();

        Assert.AreEqual(new[] {"up:child2"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseUp_StopPropagationInBubble() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isUpThisFrame = true;
        mouseState.mousePosition = new Vector2(120, 10);
        testView.InputSystem.SetMouseState(mouseState);
        root.shouldStopPropagation = true;
        testView.Update();

        Assert.AreEqual(new[] {"up:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseUp_OutOfBounds() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);

        MouseState mouseState = new MouseState();
        mouseState.leftMouseButtonState.isUpThisFrame = true;

        mouseState.mousePosition = new Vector2(1200, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        Assert.AreEqual(new string[0], root.clickList.ToArray());
    }

    [Test]
    public void MouseEnter_FiresAndPropagates() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreEnter = false;
        MouseState mouseState = new MouseState();
        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseEnter_DoesNotFireAgainForSamePosition() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreEnter = false;
        MouseState mouseState = new MouseState();
        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseEnter_DoesNotFireAgainForPositionSameElement() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreEnter = false;
        MouseState mouseState = new MouseState();
        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
        mouseState.mousePosition = new Vector2(40, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseEnter_FiresForNewElement() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreEnter = false;
        MouseState mouseState = new MouseState();
        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root"}, root.clickList.ToArray());
        mouseState.mousePosition = new Vector2(240, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();
        Assert.AreEqual(new[] {"enter:child0", "enter:root", "enter:child2"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseEnter_FiresForReEnteringElement() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreEnter = false;
        MouseState mouseState = new MouseState();

        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        mouseState.mousePosition = new Vector2(240, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        mouseState.mousePosition = new Vector2(20, 10);
        testView.InputSystem.SetMouseState(mouseState);
        testView.Update();

        Assert.AreEqual(new[] {"enter:child0", "enter:root", "enter:child2", "enter:child0"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseExit_FiresAndPropagates() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreExit = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new string[0], root.clickList.ToArray());

        testView.InputSystem.SetMousePosition(new Vector2(1200, 10));
        testView.Update();

        Assert.AreEqual(new[] {"exit:child0", "exit:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseExit_FireOnlyForExitedElement() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreExit = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new string[0], root.clickList.ToArray());

        testView.InputSystem.SetMousePosition(new Vector2(120, 10));
        testView.Update();

        Assert.AreEqual(new[] {"exit:child0"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseExit_FireAgainWhenReenteredElement() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreExit = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new string[0], root.clickList.ToArray());

        testView.InputSystem.SetMousePosition(new Vector2(120, 10));
        testView.Update();

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        testView.InputSystem.SetMousePosition(new Vector2(120, 10));
        testView.Update();

        Assert.AreEqual(new[] {"exit:child0", "exit:child0"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseMove_FiresAndPropagates() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreMove = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new[] {"move:child0", "move:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseMove_FiresAgainWhenMovedAndContains() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreMove = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new[] {"move:child0", "move:root"}, root.clickList.ToArray());

        testView.InputSystem.SetMousePosition(new Vector2(21, 10));
        testView.Update();

        Assert.AreEqual(new[] {"move:child0", "move:root", "move:child0", "move:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseMove_DoesNotFireAgainWhenNotMovedAndContains() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreMove = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new[] {"move:child0", "move:root"}, root.clickList.ToArray());
    }

    [Test]
    public void MouseHover_FiresAndPropagates() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreHover = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();
        
        Assert.AreEqual(new string[0], root.clickList.ToArray());
        testView.InputSystem.SetMousePosition(new Vector2(20, 10));

        testView.Update();

        Assert.AreEqual(new[] {"hover:child0", "hover:root"}, root.clickList.ToArray());
    }
    
    [Test]
    public void MouseHover_DoesNotFireAfterMove() {
        MockApplication testView = new MockApplication(typeof(InputSystemTestThing2));
        testView.SetViewportRect(new Rect(0, 0, 1000, 1000));
        InputSystemTestThing2 root = (InputSystemTestThing2) testView.RootElement.GetChild(0);
        root.ignoreHover = false;

        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();
        
        Assert.AreEqual(new string[0], root.clickList.ToArray());
        
        testView.InputSystem.SetMousePosition(new Vector2(20, 10));
        testView.Update();

        Assert.AreEqual(new[] {"hover:child0", "hover:root"}, root.clickList.ToArray());
        
        testView.InputSystem.SetMousePosition(new Vector2(21, 10));
        testView.Update();
        
        Assert.AreEqual(new[] {"hover:child0", "hover:root"}, root.clickList.ToArray());

    }
}
