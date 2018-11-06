﻿using System;
using System.Collections.Generic;
using Src.Animation;
using Src.Rendering;
using Src.StyleBindings;
using UnityEngine;

namespace Src.Systems {

    public class StyleSystem : IStyleSystem {

        protected readonly StyleAnimator animator;
        
        public event Action<UIElement, string> onTextContentChanged;
        public event Action<UIElement, StyleProperty> onStylePropertyChanged;

        public StyleSystem() {
            this.animator = new StyleAnimator();
        }

        public void PlayAnimation(UIStyleSet styleSet, StyleAnimation animation, AnimationOptions overrideOptions = default(AnimationOptions)) {
            int animationId = animator.PlayAnimation(styleSet, animation, overrideOptions);
        }

        public void SetViewportRect(Rect viewport) {
            animator.SetViewportRect(viewport);
        }

        public void OnReset() {
            animator.Reset();
        }

        public void OnElementCreatedFromTemplate(UIElement element) {


            if ((element.flags & UIElementFlags.TextElement) != 0) {
                ((UITextElement) element).onTextChanged += HandleTextChanged;
            }

            UITemplateContext context = element.templateContext;
            List<UIStyleGroup> baseStyles = element.templateRef.baseStyles;
            List<StyleBinding> constantStyleBindings = element.templateRef.constantStyleBindings;

            element.style.styleSystem = this;
            
            // todo -- push to style buffer & apply later on first run
            for (int i = 0; i < constantStyleBindings.Count; i++) {
                constantStyleBindings[i].Apply(element.style, context);
            }

            for (int i = 0; i < baseStyles.Count; i++) {
                element.style.AddStyleGroup(baseStyles[i]);
            }

            element.style.Initialize();

            for (int i = 0; i < element.children.Length; i++) {
                OnElementCreatedFromTemplate(element.children[i]);
            }
        }

        public void OnUpdate() {
            animator.OnUpdate();
        }

        public void OnDestroy() { }

        public void OnReady() { }

        public void OnInitialize() { }

        public void OnElementEnabled(UIElement element) { }

        public void OnElementDisabled(UIElement element) { }

        public void OnElementDestroyed(UIElement element) { }

        // todo -- buffer & flush these instead of doing it all at once
        public void SetStyleProperty(UIElement element, StyleProperty property) {
            onStylePropertyChanged?.Invoke(element, property);
        }

        private static bool IsTextProperty(StylePropertyId propertyId) {
            int intId = (int) propertyId;
            const int start = (int) StylePropertyId.__TextPropertyStart__;
            const int end = (int) StylePropertyId.__TextPropertyEnd__;
            return intId > start && intId < end;
        }


        private void HandleTextChanged(UITextElement element, string text) {
            onTextContentChanged?.Invoke(element, text);
        }

    }

}