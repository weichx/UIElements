using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mono.Linq.Expressions;
using NUnit.Framework;
using Tests.Mocks;
using UIForia.Attributes;
using UIForia.Compilers;
using UIForia.Elements;
using UIForia.Exceptions;
using UIForia.Parsing.Expression;
using UIForia.Systems;
using UIForia.Util;
using UnityEngine;

[TestFixture]
public class TestTemplateParser {

    [Template(TemplateType.String, @"
    <UITemplate>
        <Content attr:stuff='yep'>

            <Div attr:id='hello0'/>

            <CompileTestChildElement attr:id='hello1' floatValue='4f'>

           

            </CompileTestChildElement>

            <CompileTestChildElement attr:id='hello2' floatValue='14f'/>

        </Content>
    </UITemplate>
    ")]
    public class CompileTestElement : UIElement { }

    [Template(TemplateType.String, @"
        <UITemplate>
        <Content attr:isChild='yep'>

           <Text>{floatValue}</Text>

        </Content>
        </UITemplate>
    ")]
    public class CompileTestChildElement : UIElement {

        public float floatValue;

    }

//    [Test]
//    public void ParseTemplate2() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(CompileTestElement));
//
//        Func<UIElement, TemplateScope2, CompiledTemplate, UIElement> create = compiledTemplate.Compile();
//
//        Debug.Log(PrintCode(compiledTemplate.buildExpression, false));
//
//        UIElement r = create(null, new TemplateScope2(application, new LinqBindingNode(), null), compiledTemplate);
//
//        Assert.IsInstanceOf<CompileTestElement>(r);
//        Assert.AreEqual(3, r.children.size);
//        Assert.IsInstanceOf<UIDivElement>(r.children[0]);
//        Assert.IsInstanceOf<CompileTestChildElement>(r.children[1]);
//        Assert.IsInstanceOf<CompileTestChildElement>(r.children[2]);
//        Assert.AreEqual(1, r.attributes.size);
//        Assert.AreEqual("stuff", r.attributes[0].name);
//        Assert.AreEqual("yep", r.attributes[0].value);
//        Assert.AreEqual(1, r.children[0].attributes.size);
//        Assert.AreEqual(2, r.children[1].attributes.size);
//        Assert.AreEqual(2, r.children[2].attributes.size);
//
//        Assert.AreEqual("id", r.children[0].attributes[0].name);
//        Assert.AreEqual("hello0", r.children[0].attributes[0].value);
//
//        Assert.AreEqual("id", r.children[1].attributes[0].name);
//        Assert.AreEqual("hello1", r.children[1].attributes[0].value);
//        Assert.AreEqual("isChild", r.children[1].attributes[1].name);
//        Assert.AreEqual("yep", r.children[1].attributes[1].value);
//
//        Assert.AreEqual("id", r.children[2].attributes[0].name);
//        Assert.AreEqual("hello2", r.children[2].attributes[0].value);
//        Assert.AreEqual("isChild", r.children[2].attributes[1].name);
//        Assert.AreEqual("yep", r.children[2].attributes[1].value);
//
//        UIElement element = application.CreateElementFromPool(typeof(CompileTestElement), null, compiledTemplate.childCount);
//        create(element, new TemplateScope2(application, new LinqBindingNode(), null), compiledTemplate);
//    }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <Div>
//                <Text>Outer Content</Text>
//                <DefineSlot:Slot0>
//                    <Text>Default Slot0 Content</Text>
//                    <DefineSlot:Slot1>
//                        <Text>Default Slot1 Content</Text>
//                    </DefineSlot:Slot1>
//                </DefineSlot:Slot0>
//            </Div>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateWithNestedSlots : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithNestedSlots>
//                <Slot:Slot1>
//                    <Text>Replaced Slot1 Content</Text>
//                </Slot:Slot1>
//            </TemplateWithNestedSlots>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateReplaceInnerSlot : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithNestedSlots>
//                <Slot:Slot0>
//                    <Text>Replaced Slot0 Content</Text>
//                </Slot:Slot0>
//            </TemplateWithNestedSlots>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateReplaceOuterSlot : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithNestedSlots>
//                <Slot:Slot0>
//                    <Text>Replaced Slot0 Content</Text>
//                </Slot:Slot0>
//                <Slot:Slot1>
//                    <Text>Replaced Slot1 Content</Text>
//                </Slot:Slot1>
//            </TemplateWithNestedSlots>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateReplaceInnerAndOuterSlot : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithSlots>
//                <Slot:Slot0>
//                    <Text>Replaced Slot0 Content</Text>
//                </Slot:Slot0>
//            </TemplateWithSlots>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateUsingSlots : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <Text>Outer Content</Text>
//            <DefineSlot:Slot0>
//                <Text>Default SlotContent</Text>
//            </DefineSlot:Slot0>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TemplateWithSlotsSimple : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithSlotsSimple>
//                <Slot:Slot0>
//                    <Text>Replaced Slot0 Content</Text>
//                </Slot:Slot0>
//                <Slot:Slot0>
//                    <Text>Replaced Slot0 Content</Text>
//                </Slot:Slot0>
//            </TemplateWithSlotsSimple>
//        </Content>
//    </UITemplate>
//    ")]
//    public class DuplicateSlotInput : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <Slot:Slot0>
//                <Text>Replaced Slot0 Content</Text>
//            </Slot:Slot0>
//        </Content>
//    </UITemplate>
//    ")]
//    public class OrphanedSlotContent : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithSlotsSimple>
//                <Slot:NotHere>
//                    <Text>Replaced NotHere Content</Text>
//                </Slot:NotHere>
//            </TemplateWithSlotsSimple>
//        </Content>
//    </UITemplate>
//    ")]
//    public class UnmatchedSlotContent : UIElement { }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//
//            <DefineSlot:TemplateSlot attr:template='true'>
//
//                <Text>Original Template Content Here</Text>
//
//            </DefineSlot:TemplateSlot>
//
//            <Div attr:id='attach-point'/>
//
//            <Children/>
//
//        </Content>
//    </UITemplate>
//    ")]
//    public class CompileAsTemplateFn : UIElement {
//        
//        public override void OnCreate() {
//            FindById("attach-point").AddChild(GetStoredTemplate("TemplateSlot"));
//        }
//
//    }
//    
//    [Test]
//    public void TestSlotTemplate() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(TemplateWithSlotsSimple));
//        compiledTemplate.Compile();
//
//        UIElement element = new TemplateWithSlotsSimple();
//
//        compiledTemplate.Create(element, new TemplateScope2(application, null, null));
//
//        AssertElementHierarchy(new ElementAssertion(typeof(TemplateWithSlotsSimple)) {
//            children = new[] {
//                new ElementAssertion(typeof(UITextElement)) {
//                    textContent = "Outer Content"
//                },
//                new ElementAssertion(typeof(UISlotContent)) {
//                    children = new[] {
//                        new ElementAssertion(typeof(UITextElement)) {
//                            textContent = "Default SlotContent"
//                        }
//                    }
//                }
//            }
//        }, element);
//    }
//
//    [Template(TemplateType.String, @"
//    <UITemplate>    
//        <Content>
//            <TemplateWithSlotsSimple>
//
//                <Slot:Slot0>
//
//                    <Text>Override SlotContent</Text>
// 
//                </Slot:Slot0>
//
//            </TemplateWithSlotsSimple>
//        </Content>
//    </UITemplate>
//    ")]
//    public class TestSimpleSlotReplace : UIElement { }
//
//    [Test]
//    public void SimpleSlotReplace() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(TestSimpleSlotReplace));
//        compiledTemplate.Compile();
//
//        UIElement element = new TestSimpleSlotReplace();
//
//        compiledTemplate.Create(element, new TemplateScope2(application, null, null));
//
//        AssertElementHierarchy(new ElementAssertion(typeof(TestSimpleSlotReplace)) {
//            children = new[] {
//                new ElementAssertion(typeof(TemplateWithSlotsSimple)) {
//                    children = new[] {
//                        new ElementAssertion(typeof(UITextElement)) {
//                            textContent = "Outer Content"
//                        },
//                        new ElementAssertion(typeof(UISlotContent)) {
//                            children = new[] {
//                                new ElementAssertion(typeof(UITextElement)) {
//                                    textContent = "Override SlotContent"
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }, element);
//    }
//
//    [Test]
//    public void NestedSlot_ReplaceInner() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(TemplateReplaceInnerSlot));
//        compiledTemplate.Compile();
//
//        UIElement element = new TemplateReplaceInnerSlot();
//
//        compiledTemplate.Create(element, new TemplateScope2(application, null, null));
//
//        AssertElementHierarchy(new ElementAssertion(typeof(TemplateReplaceInnerSlot)) {
//            children = new[] {
//                new ElementAssertion(typeof(TemplateWithNestedSlots)) {
//                    children = new[] {
//                        new ElementAssertion(typeof(UIDivElement)) {
//                            children = new[] {
//                                new ElementAssertion(typeof(UITextElement)) {
//                                    textContent = "Outer Content"
//                                },
//                                new ElementAssertion(typeof(UISlotContent)) {
//                                    children = new[] {
//                                        new ElementAssertion(typeof(UITextElement)) {
//                                            textContent = "Default Slot0 Content"
//                                        },
//                                        new ElementAssertion(typeof(UISlotContent)) {
//                                            children = new[] {
//                                                new ElementAssertion(typeof(UITextElement)) {
//                                                    textContent = "Replaced Slot1 Content"
//                                                },
//                                            }
//                                        },
//                                    },
//                                },
//                            }
//                        },
//                    }
//                }
//            }
//        }, element);
//    }
//
//    [Test]
//    public void NestedSlot_ReplaceOuter() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(TemplateReplaceOuterSlot));
//        compiledTemplate.Compile();
//
//        UIElement element = new TemplateReplaceOuterSlot();
//
//        compiledTemplate.Create(element, new TemplateScope2(application, null, null));
//
//        AssertElementHierarchy(new ElementAssertion(typeof(TemplateReplaceOuterSlot)) {
//            children = new[] {
//                new ElementAssertion(typeof(TemplateWithNestedSlots)) {
//                    children = new[] {
//                        new ElementAssertion(typeof(UIDivElement)) {
//                            children = new[] {
//                                new ElementAssertion(typeof(UITextElement)) {
//                                    textContent = "Outer Content"
//                                },
//                                new ElementAssertion(typeof(UISlotContent)) {
//                                    children = new[] {
//                                        new ElementAssertion(typeof(UITextElement)) {
//                                            textContent = "Replaced Slot0 Content"
//                                        },
//                                    },
//                                },
//                            }
//                        },
//                    }
//                }
//            }
//        }, element);
//    }
//
//    [Test]
//    public void NestedSlot_ReplaceOuterAndInner() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        TemplateParseException exception = Assert.Throws<TemplateParseException>(() => { compiler.GetCompiledTemplate(typeof(TemplateReplaceInnerAndOuterSlot)); });
//    }
//
//    [Test]
//    public void DuplicateSlotInputShouldFail() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        TemplateParseException exception = Assert.Throws<TemplateParseException>(() => { compiler.GetCompiledTemplate(typeof(DuplicateSlotInput)); });
//    }
//
//    [Test]
//    public void OrphanedSlotContentShouldFail() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        TemplateParseException exception = Assert.Throws<TemplateParseException>(() => { compiler.GetCompiledTemplate(typeof(OrphanedSlotContent)); });
//    }
//    
//    [Test]
//    public void UnmatchedSlotNamesShouldFail() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        TemplateParseException exception = Assert.Throws<TemplateParseException>(() => { compiler.GetCompiledTemplate(typeof(UnmatchedSlotContent)); });
//    }
//
//    [Test]
//    public void CompileSlotDefaultToTemplateFunction() {
//        MockApplication application = MockApplication.CreateWithoutView();
//
//        TemplateCompiler compiler = new TemplateCompiler(application);
//
//        CompiledTemplate compiledTemplate = compiler.GetCompiledTemplate(typeof(CompileAsTemplateFn));
//        compiledTemplate.Compile();
//
//        UIElement element = new CompileAsTemplateFn();
//
//        compiledTemplate.Create(element, new TemplateScope2(application, null, null));
//
//        Assert.IsNotNull(element.storedTemplates);
//        
//    }
//
//    public void AssertElementHierarchy(ElementAssertion assertion, UIElement element, UIElement parent = null) {
//        Assert.AreEqual(assertion.type, element.GetType());
//        AssertAttributesEqual(assertion.attributes, element.attributes?.ToArray());
//        if (element is UITextElement textElement) {
//            Assert.AreEqual(assertion.textContent, textElement.text);
//        }
//
//        if (assertion.children != null && element.children.size != assertion.children.Length) {
//            Assert.IsTrue(false);
//        }
//
//        if (assertion.children == null && element.children.size != 0) {
//            Assert.IsTrue(false);
//        }
//
//        Assert.AreEqual(parent, element.parent);
//        if (assertion.children != null && element.children != null) {
//            Assert.AreEqual(assertion.children.Length, element.children.size);
//            for (int i = 0; i < assertion.children.Length; i++) {
//                AssertElementHierarchy(assertion.children[i], element.children[i], element);
//            }
//        }
//    }
//
//    public void AssertAttributesEqual(ElementAttribute[] asserts, IList<ElementAttribute> elementAttributes) {
//        if (asserts == null && elementAttributes == null) {
//            return;
//        }
//
//        if (asserts != null && elementAttributes == null) {
//            Assert.IsTrue(false);
//        }
//
//        if (asserts == null && elementAttributes != null) {
//            Assert.IsTrue(false);
//        }
//
//        Assert.AreEqual(asserts.Length, elementAttributes.Count);
//        for (int i = 0; i < asserts.Length; i++) {
//            Assert.AreEqual(asserts[i].name, elementAttributes[i].name);
//            Assert.AreEqual(asserts[i].value, elementAttributes[i].value);
//        }
//    }

    [Test]
    public void ParseTextExpressions() {
        string input = "\n this is {expression} text";
        StructList<TextExpression> output = StructList<TextExpression>.Get();
        
        TextTemplateProcessor.ProcessTextExpressions(input, output);
        
        Assert.AreEqual(3, output.size);
        Assert.AreEqual("\n this is ", output[0].text);
        Assert.AreEqual(false, output[0].isExpression);
        
        Assert.AreEqual("expression", output[1].text);
        Assert.AreEqual(true, output[1].isExpression);
        
        Assert.AreEqual(" text", output[2].text);
        Assert.AreEqual(false, output[2].isExpression);
        
        output.Clear();
        TextTemplateProcessor.ProcessTextExpressions("more {expression {expr} expression }", output);
        
        Assert.AreEqual(2, output.size);
        Assert.AreEqual("more ", output[0].text);
        Assert.AreEqual(false, output[0].isExpression);
        
        Assert.AreEqual("expression {expr} expression ", output[1].text);
        Assert.AreEqual(true, output[1].isExpression);

        output.Clear();
        TextTemplateProcessor.ProcessTextExpressions(@"more \{expression {expr} expression \}", output);
        Assert.AreEqual(3, output.size);
        Assert.AreEqual("more {expression ", output[0].text);
        Assert.AreEqual(false, output[0].isExpression);
        
        Assert.AreEqual("expr", output[1].text);
        Assert.AreEqual(true, output[1].isExpression);
        
        Assert.AreEqual(" expression }", output[2].text);
        Assert.AreEqual(false, output[2].isExpression);
        
    }

    public class ElementAssertion {

        public Type type;
        public ElementAttribute[] attributes;
        public ElementAssertion[] children;
        public string textContent;

        public ElementAssertion(Type type) {
            this.type = type;
        }

    }


    private static string PrintCode(IList<Expression> expressions, bool printNamespaces = true) {
        string retn = "";
        bool old = CSharpWriter.printNamespaces;
        CSharpWriter.printNamespaces = printNamespaces;
        for (int i = 0; i < expressions.Count; i++) {
            retn += expressions[i].ToCSharpCode();
            if (i != expressions.Count - 1) {
                retn += "\n";
            }
        }

        CSharpWriter.printNamespaces = old;
        return retn;
    }

    private static string PrintCode(Expression expression, bool printNamespaces = true) {
        bool old = CSharpWriter.printNamespaces;
        CSharpWriter.printNamespaces = printNamespaces;
        string retn = expression.ToCSharpCode();
        CSharpWriter.printNamespaces = old;
        return retn;
    }

    private static void LogCode(Expression expression, bool printNamespaces = true) {
        bool old = CSharpWriter.printNamespaces;
        CSharpWriter.printNamespaces = printNamespaces;
        string retn = expression.ToCSharpCode();
        CSharpWriter.printNamespaces = old;
        Debug.Log(retn);
    }

}