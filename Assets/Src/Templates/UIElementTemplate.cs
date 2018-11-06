using System;
using System.Collections.Generic;
using System.Linq;
using Src.Elements;
using Src.Util;
using UnityEngine;

namespace Src {

    // wraps a ParsedTemplate, this is what is created by parent templates
    // properties need to be merged from the parsed template <Content/> tag (root)
    public class UIElementTemplate : UITemplate {

        private Type rootType;
        private readonly string typeName;
        private ParsedTemplate templateToExpand;
        private TemplateType templateType;

        public UIElementTemplate(string typeName, List<UITemplate> childTemplates, List<AttributeDefinition> attributes = null)
            : base(childTemplates, attributes) {
            this.typeName = typeName;
        }

        public UIElementTemplate(Type rootType, List<UITemplate> childTemplates, List<AttributeDefinition> attributes = null)
            : base(childTemplates, attributes) {
            this.rootType = rootType;
        }

        public Type RootType => rootType;

        public override bool Compile(ParsedTemplate template) {
            if (rootType == null) {
                rootType = TypeProcessor.GetType(typeName, template.imports).rawType;
            }

            templateToExpand = TemplateParser.GetParsedTemplate(rootType);
            templateToExpand.Compile();
            // todo -- make this not suck
            bindingList = bindingList ?? new List<Binding>();
            bindingList.AddRange(templateToExpand.rootElementTemplate.bindings);
            bindingList.AddRange(templateToExpand.rootElementTemplate.constantBindings);
            constantStyleBindings.AddRange(templateToExpand.rootElementTemplate.constantStyleBindings);
            baseStyles.AddRange(templateToExpand.rootElementTemplate.baseStyles);
            // todo -- remove duplicate bindings
            base.Compile(template);
            return true;
        }

        public override Type elementType => rootType;

        // children of this element are transcluded
        // actual children are built from parsed template's root children
        public override UIElement CreateScoped(TemplateScope inputScope) {
            //   Compile(null);
            // todo -- some templates don't need their own scope

            UIElement element = (UIElement) Activator.CreateInstance(rootType);
            element.flags |= UIElementFlags.TemplateRoot;
            element.templateRef = this;
            templateToExpand.Compile();
// if is root then we need use childTemplates as non transcluded
            List<UITemplate> transcludedTemplates = childTemplates;
            List<UITemplate> actualChildren = templateToExpand.childTemplates;

            UITemplateContext context = new UITemplateContext(element, null);
            TemplateScope scope = new TemplateScope(element, context);

            element.children = ArrayPool<UIElement>.GetExactSize(actualChildren.Count);

            for (int i = 0; i < element.children.Length; i++) {
                element.children[i] = actualChildren[i].CreateScoped(scope);
                element.children[i].parent = element;
                element.children[i].templateParent = element; // try to get rid of this
            }

            UIChildrenElement childrenElement = element.transcludedChildren;

            if (childrenElement != null) {
                childrenElement.children = new UIElement[transcludedTemplates.Count];
                for (int i = 0; i < childrenElement.children.Length; i++) {
                    childrenElement.children[i] = transcludedTemplates[i].CreateScoped(inputScope);
                    childrenElement.children[i].parent = childrenElement;
                    childrenElement.children[i].templateParent = element;
                }
            }

            AssignContext(element, context);

            
            // find <Slot>
            //     -> attach from input

            // todo -- not sure this is safe to overwrite bindings here probably need to merge
            // actually the only bindings allowed on <Contents> tag should be styles
            // which would make this ok. need to merge styles though, maybe input handlers though?
//            instanceData.bindings = bindings;
//            instanceData.context = inputScope.context;
//            instanceData.constantBindings = constantBindings;
//            instanceData.constantStyleBindings = constantStyleBindings;
//            instanceData.element.templateAttributes = templateAttributes;
//            
//            instanceData.baseStyles = baseStyles;
//            
//            instanceData.mouseEventHandlers = mouseEventHandlers;
//            instanceData.dragEventCreators = dragEventCreators;
//            instanceData.dragEventHandlers = dragEventHandlers;
//            instanceData.keyboardEventHandlers = keyboardEventHandlers;
//
//            outputScope.context.rootElement = instanceData.element;
//

            return element;
        }
        
        private void AssignContext(UIElement element, UITemplateContext context) {
            element.templateContext = context;
                
            if (element.children == null) return;

            for (int i = 0; i < element.children.Length; i++) {
                
                if (element.children[i].templateRef is UIElementTemplate) {
                    element.children[i].templateContext = context;
                    continue;
                }
                
                if (element.children[i] is UIChildrenElement) {
                    continue;
                }
                
                AssignContext(element.children[i], context);
            }
        }

    }

}