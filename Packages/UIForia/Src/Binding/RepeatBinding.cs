using System;
using System.Collections.Generic;
using UIForia;
using UIForia.Util;

public class RepeatBindingNode : BindingNode {

    public UITemplate template;

}

public class RepeatBindingNode<T, U> : RepeatBindingNode where T : class, IList<U>, new() {

    private UIRepeatElement<U> repeat;
    private readonly Expression<T> listExpression;
    private T previousReference;

    public RepeatBindingNode(UIRepeatElement<U> repeat, Expression<T> listExpression) {
        this.repeat = repeat;
        this.listExpression = listExpression;
    }

    public void Validate() {
        T list = listExpression.Evaluate(context);
        repeat.list = list;

        if (list == null || list.Count == 0) {
            if (previousReference == null) {
                return;
            }

            repeat.listBecameEmpty = previousReference.Count > 0;

            previousReference.Clear();
            previousReference = null;

            element.view.Application.DestroyChildren(element);
            return;
        }

        if (previousReference == null) {
            previousReference = new T();
            element.children = ArrayPool<UIElement>.GetExactSize(list.Count);

            for (int i = 0; i < list.Count; i++) {
                previousReference.Add(list[i]);
                UIElement newItem = template.CreateScoped(repeat.scope);
                newItem.parent = element;
                newItem.templateParent = element;
                // root object isn't being assigned. make it assigned 
                newItem.templateContext.rootObject = element.templateContext.rootObject;
                element.children[i] = newItem;
                element.view.Application.RegisterElement(newItem);
            }

            repeat.listBecamePopulated = true;
        }
        else if (list.Count > previousReference.Count) {
            repeat.listBecamePopulated = previousReference.Count == 0;

            UIElement[] oldChildren = element.children;

            UIElement[] ownChildren = ArrayPool<UIElement>.GetExactSize(list.Count);

            element.children = ownChildren;

            for (int i = 0; i < oldChildren.Length; i++) {
                ownChildren[i] = oldChildren[i];
            }

            int previousCount = previousReference.Count;
            int diff = list.Count - previousCount;

            for (int i = 0; i < diff; i++) {
                previousReference.Add(list[previousCount + i]);
                UIElement newItem = template.CreateScoped(repeat.scope);
                newItem.parent = element;
                newItem.templateParent = element;
                newItem.templateContext.rootObject = element.templateContext.rootObject;

                ownChildren[previousCount + i] = newItem;
                element.view.Application.RegisterElement(newItem);
            }

            ArrayPool<UIElement>.Release(ref oldChildren);
        }
        else if (previousReference.Count > list.Count) {
            // todo -- this is potentially way faster w/ a DestroyChildren(start, end) method

            int diff = previousReference.Count - list.Count;
            for (int i = 0; i < diff; i++) {
                int index = previousReference.Count - 1;
                previousReference.RemoveAt(index);
                Application.DestroyElement(element.children[index]);
            }
        }

    }

    public override void OnUpdate() {
        Validate();

        if (!element.isEnabled || element.children == null || previousReference == null) {
            return;
        }

        if (bindings == null) return;
        
        for (int i = 0; i < bindings.Length; i++) {
            bindings[i].Execute(element, context);
        }

    }

}