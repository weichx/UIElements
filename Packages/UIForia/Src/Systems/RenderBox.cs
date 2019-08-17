using System;
using UIForia.Elements;
using UIForia.Layout;
using UIForia.Util;
using UnityEngine;

namespace UIForia.Rendering {

    public struct PolyRect {

        public Vector2 p0;
        public Vector2 p1;
        public Vector2 p2;
        public Vector2 p3;

        public PolyRect(in Vector2 p0, in Vector2 p1, in Vector2 p2, in Vector2 p3) {
            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

    }

    public abstract class RenderBox {

        internal string uniqueId;

        protected internal UIElement element;

        public Visibility visibility;
        public Overflow overflowX;
        public Overflow overflowY;
        public ClipBehavior clipBehavior = ClipBehavior.Normal;
        public bool culled;
        public Vector4 clipRect;
        public bool hasForeground;
        public int zIndex;
        public int layer;

        internal ClipData clipper;
        public Texture clipTexture;
        public Vector4 clipUVs;
        public bool didRender;
        protected ClipShape clipShape;

        public virtual Rect RenderBounds => new Rect(
            element.layoutResult.localPosition.x,
            element.layoutResult.localPosition.y,
            element.layoutResult.actualSize.width,
            element.layoutResult.actualSize.height
        );

        public virtual Rect ClipBounds => RenderBounds;

        public virtual void OnInitialize() {
            overflowX = element.style.OverflowX;
            overflowY = element.style.OverflowY;
        }

        public virtual void OnDestroy() { }

        public virtual void OnStylePropertyChanged(StructList<StyleProperty> propertyList) {
            for (int i = 0; i < propertyList.size; i++) {
                ref StyleProperty property = ref propertyList.array[i];
                switch (property.propertyId) {
                    case StylePropertyId.OverflowX:
                        overflowX = property.AsOverflow;
                        break;
                    case StylePropertyId.OverflowY:
                        overflowY = property.AsOverflow;
                        break;
                }
            }
        }

        public abstract void PaintBackground(RenderContext ctx);

        public virtual void PrePaintText() { }

        public virtual void PrePaintTexture() { }

        public virtual void PaintForeground(RenderContext ctx) { }

        public virtual bool ShouldCull(in Rect bounds) {
            // can probably optimize rounded case & say if not in padding bounds, fail
            return false; //RectExtensions.ContainOrOverlap(this.RenderBounds, bounds);
        }

        public static float ResolveFixedSize(UIElement element, float baseSize, UIFixedLength length) {
            switch (length.unit) {
                case UIFixedUnit.Unset:
                case UIFixedUnit.Pixel:
                    return length.value;
                case UIFixedUnit.Percent:
                    return baseSize * length.value;
                case UIFixedUnit.Em:
                    return element.style.GetResolvedFontSize() * length.value;
                case UIFixedUnit.ViewportWidth:
                    return element.View.Viewport.width * length.value;
                case UIFixedUnit.ViewportHeight:
                    return element.View.Viewport.height * length.value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public virtual ClipShape GetClipShape() {
            clipShape = clipShape ?? new ClipShape();

//            clipShape.SetCornerClip();
//            clipShape.SetCornerRadii();
//            clipShape.SetFromMesh(mesh);
//            clipShape.SetFromElement(element);
//            clipShape.SetFromEllipse();
//            clipShape.SetFromRect();
//            clipShape.SetFromCircle();
//            clipShape.SetFromDiamond();
//            clipShape.SetFromTriangle();
//            clipShape.SetTexture(texture, channel);

            clipShape.SetFromElement(element);

            return clipShape;
        }

    }

}