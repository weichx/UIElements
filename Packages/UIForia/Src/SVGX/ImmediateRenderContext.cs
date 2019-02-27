using System.Collections.Generic;
using TMPro;
using UIForia.Text;
using UIForia.Util;
using UnityEngine;

namespace SVGX {

    // todo this saves 2 list allocations + operations
    public struct SVGXDrawState {

        public SVGXMatrix matrix;
        public SVGXStyle style;

        public Vector2 lastPoint;
        // clip state lives here too

    }

    public enum StrokeMode {

        Uniform,
        Varying

    }

    public class ImmediateRenderContext {

        internal readonly LightList<Vector2> points;
        internal readonly LightList<SVGXStyle> styles;
        internal readonly LightList<SVGXMatrix> transforms;
        internal readonly LightList<SVGXDrawCall> drawCalls;
        internal readonly LightList<SVGXShape> shapes;
        internal readonly LightList<SVGXClipGroup> clipGroups;
        internal readonly Stack<int> clipStack;
        internal readonly LightList<SVGXGradient> gradients;
        internal readonly LightList<Texture2D> textures;
        internal readonly LightList<TextInfo> textInfos;

        private Vector2 lastPoint;
        private SVGXMatrix currentMatrix;
        private SVGXStyle currentStyle;
        private RangeInt currentShapeRange;
        private SVGXGradient currentGradient;
        private Texture2D currentTexture;
        private StrokeMode strokeMode;

        public ImmediateRenderContext() {
            points = new LightList<Vector2>(128);
            styles = new LightList<SVGXStyle>();
            transforms = new LightList<SVGXMatrix>();
            currentMatrix = SVGXMatrix.identity;
            drawCalls = new LightList<SVGXDrawCall>();
            shapes = new LightList<SVGXShape>();
            clipGroups = new LightList<SVGXClipGroup>();
            gradients = new LightList<SVGXGradient>();
            shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
            textures = new LightList<Texture2D>();
            clipStack = new Stack<int>();
            textInfos = new LightList<TextInfo>();
            currentStyle = SVGXStyle.Default();
            strokeMode = StrokeMode.Uniform;
        }

        public void SetFill(Color color) {
            currentStyle.fillMode = FillMode.Color;
            currentStyle.fillColor = color;
        }

        public void SetFill(SVGXGradient gradient) {
            currentStyle.fillMode = FillMode.Gradient;
            currentGradient = gradient;
        }

        public void SetFill(Texture2D texture, Color tintColor) {
            currentStyle.fillMode = FillMode.TextureTint;
            currentStyle.fillTintColor = tintColor;
            currentTexture = texture;
        }

        public void SetFill(Texture2D texture, SVGXGradient gradient) {
            currentStyle.fillMode = FillMode.TextureGradient;
            currentStyle.gradientId = gradient.id;
            currentTexture = texture;
            currentGradient = gradient;
        }

        public void SetFill(Texture2D texture) {
            currentStyle.fillMode = FillMode.Texture;
            currentStyle.fillTintColor = Color.white;
            currentStyle.textureId = texture.GetInstanceID();
            currentTexture = texture;
        }

        public void SetStrokeColor(Color color) {
            this.currentStyle.strokeColor = color;
        }

        public void MoveTo(float x, float y) {
            lastPoint = new Vector2(x, y);
            SVGXShape currentShape = shapes[shapes.Count - 1];
            if (currentShape.type != SVGXShapeType.Unset) {
                shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
                currentShapeRange.length++;
            }
        }

        public void Text(float x, float y, TextInfo text) {
            SVGXShape currentShape = shapes[shapes.Count - 1];

            // todo -- bounds will depend on text layout, should we just do it here?
            SVGXShape textShape = new SVGXShape(SVGXShapeType.Text, new RangeInt(points.Count, 1), new SVGXBounds(), false, textInfos.Count);
            textInfos.Add(text);

            if (currentShape.type == SVGXShapeType.Unset) {
                shapes[shapes.Count - 1] = textShape;
            }
            else {
                shapes.Add(textShape);
            }

            currentShapeRange.length++;
            lastPoint = new Vector2(x, y);
            
            points.Add(lastPoint);
        }

        public void LineTo(float x, float y) {
            SVGXShape currentShape = shapes[shapes.Count - 1];

            Vector2 point = new Vector2(x, y);

            switch (currentShape.type) {
                case SVGXShapeType.Path:
                    currentShape.pointRange.length++;
                    shapes[shapes.Count - 1] = currentShape;
                    break;
                case SVGXShapeType.Unset:
                    currentShape = new SVGXShape(SVGXShapeType.Path, new RangeInt(points.Count, 2));
                    shapes[shapes.Count - 1] = currentShape;
                    currentShapeRange.length++;
                    points.Add(lastPoint);
                    break;
                default:
                    currentShape = new SVGXShape(SVGXShapeType.Path, new RangeInt(points.Count, 2));
                    shapes.Add(currentShape);
                    points.Add(lastPoint);
                    currentShapeRange.length++;
                    break;
            }

            lastPoint = point;
            points.Add(point);
        }

        public void HorizontalLineTo(float x) {
            LineTo(x, lastPoint.y);
        }

        public void VerticalLineTo(float y) {
            LineTo(lastPoint.x, y);
        }

        public void ArcTo(float rx, float ry, float angle, bool isLargeArc, bool isSweepArc, float endX, float endY) {
            Vector2 end = new Vector2(endX, endY);

            int pointStart = points.Count;
            int pointCount = SVGXBezier.Arc(points, lastPoint, rx, ry, angle, isLargeArc, isSweepArc, end);
            UpdateShape(pointStart, pointCount);
            lastPoint = end;
        }

        public void ClosePath() {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            if (currentShape.type != SVGXShapeType.Path) {
                return;
            }

            Vector2 startPoint = points[currentShape.pointRange.start];
            LineTo(startPoint.x, startPoint.y);
            currentShape.isClosed = true;
            shapes[shapes.Count - 1] = currentShape;
            shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
            lastPoint = startPoint;
        }

        public void CubicCurveTo(Vector2 ctrl0, Vector2 ctrl1, Vector2 end) {
            int pointStart = points.Count;
            int pointCount = SVGXBezier.CubicCurve(points, lastPoint, ctrl0, ctrl1, end);
            UpdateShape(pointStart, pointCount);
            lastPoint = end;
        }

        public void QuadraticCurveTo(Vector2 ctrl, Vector2 end) {
            int pointStart = points.Count;
            int pointCount = SVGXBezier.QuadraticCurve(points, lastPoint, ctrl, end);
            UpdateShape(pointStart, pointCount);

            lastPoint = end;
        }

        public void RoundedRect(Rect rect, float rtl, float rtr, float rbl, float rbr) {
            SVGXShapeType lastType = shapes[shapes.Count - 1].type;

            int pointRangeStart = points.Count;

            points.Add(rect.min);
            points.Add(new Vector2(rect.width, rect.height));
            points.Add(new Vector2(rtl, rtr));
            points.Add(new Vector2(rbl, rbr));

            RangeInt pointRange = new RangeInt(pointRangeStart, points.Count - pointRangeStart);
            SVGXShape currentShape = new SVGXShape(SVGXShapeType.RoundedRect, pointRange, new SVGXBounds(rect.min, rect.max), true);

            if (lastType != SVGXShapeType.Unset) {
                shapes.Add(currentShape);
            }
            else {
                shapes[shapes.Count - 1] = currentShape;
            }

            lastPoint = points[points.Count - 1];
            currentShapeRange.length++;
        }

        // todo -- diamond / other sdf shapes

        private void UpdateShape(int pointStart, int pointCount) {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            switch (currentShape.type) {
                case SVGXShapeType.Path:
                    currentShape.pointRange.length += pointCount;
                    shapes[shapes.Count - 1] = currentShape;
                    break;
                case SVGXShapeType.Unset:
                    currentShape = new SVGXShape(SVGXShapeType.Path, new RangeInt(pointStart, pointCount));
                    shapes[shapes.Count - 1] = currentShape;
                    currentShapeRange.length++;
                    break;
                default:
                    currentShape = new SVGXShape(SVGXShapeType.Path, new RangeInt(pointStart, pointCount));
                    shapes.Add(currentShape);
                    currentShapeRange.length++;
                    break;
            }
        }

        public void Clear() {
            points.Clear();
            styles.Clear();
            transforms.Clear();
            drawCalls.Clear();
            shapes.Clear();
            currentStyle = SVGXStyle.Default();
            currentMatrix = SVGXMatrix.identity;
            lastPoint = Vector2.zero;
            shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
            currentShapeRange = new RangeInt();
            gradients.Clear();
            textures.Clear();
            clipStack.Clear();
            clipGroups.Clear();
            textInfos.Clear();
            strokeMode = StrokeMode.Uniform;
        }

        public void Save() {
            transforms.Add(currentMatrix);
            styles.Add(currentStyle);
        }

        public void Restore() {
            if (transforms.Count > 0) {
                currentMatrix = transforms.RemoveLast();
            }

            if (styles.Count > 0) {
                currentStyle = styles.RemoveLast();
            }
        }

        internal SVGXClipGroup GetClipGroup(int id) {
            if (id >= 0 && id < clipGroups.Count) {
                return clipGroups[id];
            }

            return default;
        }

        public void PushClip() {
            int parentId = clipStack.Count > 0 ? clipStack.Peek() : -1;
            SVGXClipGroup clipGroup = new SVGXClipGroup(parentId, currentShapeRange);
            clipStack.Push(clipGroups.Count);
            clipGroups.Add(clipGroup);
            BeginPath();
        }

        public void PopClip() {
            if (clipStack.Count > 0) {
                clipStack.Pop();
            }
        }

        public void Rect(float x, float y, float width, float height) {
            SimpleShape(SVGXShapeType.Rect, x, y, width, height);
        }

        public void Ellipse(float x, float y, float dx, float dy) {
            SimpleShape(SVGXShapeType.Ellipse, x, y, dx, dy);
        }

        public void Circle(float x, float y, float radius) {
            SimpleShape(SVGXShapeType.Circle, x, y, radius * 2f, radius * 2f);
        }

        public void CircleFromCenter(float cx, float cy, float radius) {
            SimpleShape(SVGXShapeType.Circle, cx - radius, cy - radius, radius * 2f, radius * 2f);
        }

        public void FillRect(float x, float y, float width, float height) {
            BeginPath();
            Rect(x, y, width, height);
            Fill();
            BeginPath();
        }

        public void FillRect(Rect rect) {
            BeginPath();
            Rect(rect.x, rect.y, rect.width, rect.height);
            Fill();
            BeginPath();
        }

        public void FillCircle(float x, float y, float radius) {
            BeginPath();
            Circle(x, y, radius);
            Fill();
            BeginPath();
        }

        public void FillEllipse(float x, float y, float dx, float dy) {
            BeginPath();
            Ellipse(x, y, dx, dy);
            Fill();
            BeginPath();
        }

        private void SimpleShape(SVGXShapeType shapeType, float x, float y, float width, float height) {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            SVGXShapeType lastType = currentShape.type;

            Vector2 x0y0 = new Vector2(x, y);
            Vector2 x1y1 = new Vector2(width, height);

            currentShape = new SVGXShape(shapeType, new RangeInt(points.Count, 2), new SVGXBounds(x0y0, x0y0 + x1y1));

            points.Add(x0y0);
            points.Add(x1y1);

            currentShape.isClosed = true;

            if (lastType != SVGXShapeType.Unset) {
                shapes.Add(currentShape);
            }
            else {
                shapes[shapes.Count - 1] = currentShape;
            }

            currentShapeRange.length++;
        }

        public void BeginPath(StrokeMode strokeMode = StrokeMode.Uniform) {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            if (currentShape.type != SVGXShapeType.Unset) {
                shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
                currentShapeRange = new RangeInt(shapes.Count - 1, 0);
            }
        }

        public void Fill() {
            if ((currentStyle.fillMode & FillMode.Texture) != 0) {
                if (!textures.Contains(currentTexture)) {
                    textures.Add(currentTexture);
                }

                currentStyle.textureId = currentTexture.GetInstanceID();
            }

            if ((currentStyle.fillMode & FillMode.Gradient) != 0) {
                if (!gradients.Contains(currentGradient)) {
                    gradients.Add(currentGradient);
                }

                currentStyle.gradientId = currentGradient.id;
            }

            int clipId = clipStack.Count > 0 ? clipStack.Peek() : -1;
            drawCalls.Add(new SVGXDrawCall(DrawCallType.StandardFill, clipId, currentStyle, currentMatrix, currentShapeRange));
        }

        public void Shadow() {
            int clipId = clipStack.Count > 0 ? clipStack.Peek() : -1;
            drawCalls.Add(new SVGXDrawCall(DrawCallType.Shadow, clipId, currentStyle, currentMatrix, currentShapeRange));
        }

        public void Stroke() {
            int clipId = clipStack.Count > 0 ? clipStack.Peek() : -1;
            drawCalls.Add(new SVGXDrawCall(DrawCallType.StandardStroke, clipId, currentStyle, currentMatrix, currentShapeRange));
        }

        public void SetStrokeOpacity(float opacity) {
            currentStyle.strokeOpacity = opacity;
        }

        public void SetStrokeWidth(float width) {
            // todo -- support varying stroke parameters per path call
            currentStyle.strokeWidth = width;
        }

        public void SetTransform(SVGXMatrix trs) {
            currentMatrix = trs;
        }

        public void SaveState() { }

        public void RestoreState() { }

        public void SetFillOpacity(float fillOpacity) {
            currentStyle.fillOpacity = fillOpacity;
        }

        public void SetStrokePlacement(StrokePlacement strokePlacement) {
            currentStyle.strokePlacement = strokePlacement;
        }

        public void SetShadowColor(Color shadowColor) {
            currentStyle.shadowColor = shadowColor;
        }

        public void SetShadowOffsetX(float shadowOffsetX) {
            currentStyle.shadowOffsetX = shadowOffsetX;
        }

        public void SetShadowOffsetY(float shadowOffsetY) {
            currentStyle.shadowOffsetY = shadowOffsetY;
        }

        public void SetShadowSoftness(float shadowSoftness) {
            currentStyle.shadowSoftnessX = Mathf.Clamp01(shadowSoftness);
            currentStyle.shadowSoftnessY = Mathf.Clamp01(shadowSoftness);
        }

        public void SetShadowSoftnessX(float shadowSoftnessX) {
            currentStyle.shadowSoftnessX = Mathf.Clamp01(shadowSoftnessX);
        }

        public void SetShadowSoftnessY(float shadowSoftnessY) {
            currentStyle.shadowSoftnessY = Mathf.Clamp01(shadowSoftnessY);
        }

        public void SetShadowIntensity(float shadowIntensity) {
            currentStyle.shadowIntensity = shadowIntensity;
        }

        public void SetShadowTint(Color shadowTint) {
            currentStyle.shadowTint = shadowTint;
        }

    }

}