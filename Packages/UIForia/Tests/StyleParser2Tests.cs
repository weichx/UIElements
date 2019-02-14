using NUnit.Framework;
using UIForia.Parsing.Style;
using UIForia.Parsing.Style.AstNodes;

[TestFixture]
public class StyleParser2Tests {

    [Test]
    public void ParseSimpleStyle() {
        var nodes = StyleParser2.Parse(@"
            style simple {
                MarginTop = 10px;
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        var rootNode = ((StyleRootNode) nodes[0]);
        Assert.AreEqual("simple", rootNode.identifier);
        Assert.AreEqual(null, rootNode.tagName);
        Assert.AreEqual(1, rootNode.children.Count);

        var propertyNode = rootNode.children[0];
        Assert.AreEqual(StyleASTNodeType.Property, propertyNode.type);
        
        var typedPropertyNode = (((PropertyNode) propertyNode));
        Assert.AreEqual("MarginTop", typedPropertyNode.propertyName);
        Assert.AreEqual(StyleASTNodeType.Measurement, typedPropertyNode.propertyValue.type);
    }

    [Test]
    public void ParseColorProperty() {
        var nodes = StyleParser2.Parse(@"
            style withBg {
                BackgroundColor = rgba(10, 20, 30, 40);
            }
        ");
        
        Assert.AreEqual(1, nodes.Count);
        var rootNode = ((StyleRootNode) nodes[0]);
        Assert.AreEqual("withBg", rootNode.identifier);
        Assert.AreEqual(null, rootNode.tagName);
        Assert.AreEqual(1, rootNode.children.Count);

        var property = (((PropertyNode) rootNode.children[0]));
        Assert.AreEqual("BackgroundColor", property.propertyName);
        Assert.AreEqual(StyleASTNodeType.Rgba, property.propertyValue.type);

        var rgbaNode = (RgbaNode) property.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Rgba, rgbaNode.type);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("10"), rgbaNode.red);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("20"), rgbaNode.green);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("30"), rgbaNode.blue);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("40"), rgbaNode.alpha);
    }

    [Test]
    public void ParseRgbColorProperty() {
        var nodes = StyleParser2.Parse(@"
            style withBg {
                BackgroundColor = rgb(10, 20, 30);
            }
        ");
        
        Assert.AreEqual(1, nodes.Count);
        var rootNode = ((StyleRootNode) nodes[0]);
        Assert.AreEqual("withBg", rootNode.identifier);
        Assert.AreEqual(null, rootNode.tagName);
        Assert.AreEqual(1, rootNode.children.Count);

        var property = (((PropertyNode) rootNode.children[0]));
        Assert.AreEqual("BackgroundColor", property.propertyName);
        Assert.AreEqual(StyleASTNodeType.Rgb, property.propertyValue.type);

        var rgbNode = (RgbNode) property.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Rgb, rgbNode.type);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("10"), rgbNode.red);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("20"), rgbNode.green);
        Assert.AreEqual(StyleASTNode.NumericLiteralNode("30"), rgbNode.blue);
    }

    [Test]
    public void ParseUrl() {
        var nodes = StyleParser2.Parse(@"
            style withBg {
                Background = url(path/to/image);
            }
        ");
        
        Assert.AreEqual(1, nodes.Count);
        var rootNode = ((StyleRootNode) nodes[0]);
        Assert.AreEqual("withBg", rootNode.identifier);
        Assert.AreEqual(null, rootNode.tagName);
        Assert.AreEqual(1, rootNode.children.Count);

        var property = (((PropertyNode) rootNode.children[0]));
        Assert.AreEqual("Background", property.propertyName);
        Assert.AreEqual(StyleASTNodeType.Url, property.propertyValue.type);

        var urlNode = (UrlNode) property.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Url, urlNode.type);
        Assert.AreEqual(StyleASTNode.IdentifierNode("path/to/image"), urlNode.url);
    }

    [Test]
    public void ParsePropertyWithReference() {
        var nodes = StyleParser2.Parse(@"
            style hasReferenceToBackgroundImagePath {
                Background = url(@pathRef);
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        var property = (((PropertyNode) ((StyleRootNode) nodes[0]).children[0]));
        Assert.AreEqual("Background", property.propertyName);
        Assert.AreEqual(StyleASTNodeType.Url, property.propertyValue.type);

        var urlNode = (UrlNode) property.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Url, urlNode.type);
        Assert.AreEqual(StyleASTNode.ReferenceNode("pathRef"), urlNode.url);
    }

    [Test]
    public void ParseStyleState() {
        var nodes = StyleParser2.Parse(@"
            style hasBackgroundOnHover {
                [hover] { Background = url(@pathRef.member); }
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        var stateGroupContainer = (((StyleStateContainer) ((StyleRootNode) nodes[0]).children[0]));
        Assert.AreEqual("hover", stateGroupContainer.identifier);
        
        var property = (PropertyNode) stateGroupContainer.children[0];
        Assert.AreEqual("Background", property.propertyName);

        var urlNode = (UrlNode) property.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Url, urlNode.type);
        Assert.AreEqual(StyleASTNodeType.Reference, urlNode.url.type);
        var refNode = (ReferenceNode) urlNode.url;
        Assert.AreEqual("pathRef", refNode.referenceName);
        Assert.AreEqual(1, refNode.children.Count);
        Assert.AreEqual(StyleASTNodeType.DotAccess, refNode.children[0].type);
        var dotAccess = (DotAccessNode) refNode.children[0];
        Assert.AreEqual("member", dotAccess.propertyName);
    }

    [Test]
    public void ParseAttributeGroup() {
        var nodes = StyleParser2.Parse(@"
            style hasBackgroundOnHover {
                [attr:attrName] { Background = url(@pathRef); }
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        var attributeGroupContainer = (((AttributeGroupContainer) ((StyleRootNode) nodes[0]).children[0]));
        Assert.AreEqual("attrName", attributeGroupContainer.identifier);
        
        var property = (PropertyNode) attributeGroupContainer.children[0];
        Assert.AreEqual("Background", property.propertyName);
        Assert.AreEqual(StyleASTNodeType.Url, property.propertyValue.type);

        var urlNode = (UrlNode) property.propertyValue;
        Assert.AreEqual(StyleASTNode.ReferenceNode("pathRef"), urlNode.url);
    }
    
    [Test]
    public void ParseEmptyGroups() {
        var nodes = StyleParser2.Parse(@"
            style hasBackgroundOnHover {
                [attr:attrName] { }
                [hover] {}
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        Assert.AreEqual(2, ((StyleRootNode) nodes[0]).children.Count);
        
        var attributeGroupContainer = (((AttributeGroupContainer) ((StyleRootNode) nodes[0]).children[0]));
        var stateGroupContainer = (((StyleStateContainer) ((StyleRootNode) nodes[0]).children[1]));
        Assert.AreEqual("attrName", attributeGroupContainer.identifier);
        Assert.AreEqual(0, attributeGroupContainer.children.Count);
        
        Assert.AreEqual("hover", stateGroupContainer.identifier);
        Assert.AreEqual(0, stateGroupContainer.children.Count);
    }

    [Test]
    public void ParseAttributeGroupWithStateGroup() {
        var nodes = StyleParser2.Parse(@"
            style mixingItAllUp {
                TextColor = green;
                [attr:attrName] { 
                    Background = url(@pathRef); 
                    [hover] {
                        TextColor = red;
                        TextColor = yellow;
                    }
                    TextColor = blue;
                }
                MarginTop = 10px;
            }
            style mixingItAllUp2 {
                TextColor = green;
                [attr:attrName] { 
                    Background = url(@pathRef); 
                    [hover] {
                        TextColor = red;
                    }
                    TextColor = blue;
                }
                MarginTop = 10px;
            }
        ");

        // there should be two style nodes
        Assert.AreEqual(2, nodes.Count);
        
        // ...3 nodes in a style
        var styleChildren = ((StyleRootNode) nodes[0]).children;
        Assert.AreEqual(3, styleChildren.Count);

        
        // first node is the property color = green
        var property1 = (PropertyNode) styleChildren[0];
        Assert.AreEqual("TextColor", property1.propertyName);
        Assert.AreEqual(StyleASTNodeType.Identifier, property1.propertyValue.type);

        // next the attribute group that in turn has 3 children
        var attributeGroupContainer = (((AttributeGroupContainer) styleChildren[1]));
        Assert.AreEqual("attrName", attributeGroupContainer.identifier);
        
        // and the trailing margin property is the third of the style's properties 
        var property2 = (PropertyNode) styleChildren[2];
        Assert.AreEqual("MarginTop", property2.propertyName);

        // now assert the existence of the three attribute group children
        Assert.AreEqual(3, attributeGroupContainer.children.Count);
        var attrProperty1 = (PropertyNode) attributeGroupContainer.children[0];
        var stateGroup = (StyleStateContainer) attributeGroupContainer.children[1];
        var attrProperty2 = (PropertyNode) attributeGroupContainer.children[2];

        // assert values for attr property 1
        Assert.AreEqual("Background", attrProperty1.propertyName);

        var urlNode = (UrlNode) attrProperty1.propertyValue;
        Assert.AreEqual(StyleASTNodeType.Url, urlNode.type);
        Assert.AreEqual(StyleASTNode.ReferenceNode("pathRef"), urlNode.url);
        
        // assert values for attr property 2
        Assert.AreEqual("TextColor", attrProperty2.propertyName);
        Assert.AreEqual(StyleASTNodeType.Identifier, attrProperty2.propertyValue.type);
        
        // assert the state group
        Assert.AreEqual("hover", stateGroup.identifier);
        // just asserting that multiple properties in a state group can be a thing
        Assert.AreEqual(2, stateGroup.children.Count);
        var stateGroupChild = (PropertyNode) stateGroup.children[0];
        Assert.AreEqual("TextColor", stateGroupChild.propertyName);
        Assert.AreEqual(StyleASTNodeType.Identifier, stateGroupChild.propertyValue.type);
    }


    [Test]
    public void ParseExportKeyword() {
        var nodes = StyleParser2.Parse(@"
            export const color0 : Color = rgba(1, 0, 0, 1);
        ");
        
        // there should be two style nodes
        Assert.AreEqual(1, nodes.Count);
        Assert.AreEqual(StyleASTNodeType.Export, nodes[0].type);

        ExportNode exportNode = (ExportNode) nodes[0];
        Assert.AreEqual("color0", exportNode.constNode.constName);
        Assert.AreEqual("Color", exportNode.constNode.constType);
        Assert.AreEqual(StyleASTNodeType.Rgba, exportNode.constNode.value.type);
    }
    
    [Test]
    public void ParseMultipleAttributes() {
        var nodes = StyleParser2.Parse(@"
            style hasBackgroundOnHover {
                not [attr:attrName1] and not [attr:attrName2] and [attr:attrName3]{ }
            }
        ");

        Assert.AreEqual(1, nodes.Count);
        StyleRootNode styleRootNode = (StyleRootNode) nodes[0];
        Assert.AreEqual(1, styleRootNode.children.Count);
        
        var attributeGroupContainer3 = (((AttributeGroupContainer) styleRootNode.children[0]));
        Assert.AreEqual("attrName3", attributeGroupContainer3.identifier);
        Assert.AreEqual(false, attributeGroupContainer3.invert);
        
        var attributeGroupContainer2 = attributeGroupContainer3.next;
        Assert.AreEqual("attrName2", attributeGroupContainer2.identifier);
        Assert.AreEqual(true, attributeGroupContainer2.invert);
        
        var attributeGroupContainer1 = attributeGroupContainer2.next;
        Assert.AreEqual("attrName1", attributeGroupContainer1.identifier);
        Assert.AreEqual(true, attributeGroupContainer1.invert);
        Assert.IsNull(attributeGroupContainer1.next);
    }
}
