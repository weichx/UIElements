﻿using System;
using UIForia.Animation;
using UIForia.Rendering;
using UnityEngine;

namespace UIForia.Systems {

    public interface IStyleSystem : ISystem {

        event Action<UIElement, string> onTextContentChanged;
        event Action<UIElement, StyleProperty> onStylePropertyChanged;

        void SetStyleProperty(UIElement element, StyleProperty propertyValue);

        void PlayAnimation(UIStyleSet styleSet, StyleAnimation animation, AnimationOptions options);

        void SetViewportRect(Rect viewport);

    }

}