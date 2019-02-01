using System.Collections.Generic;
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

    public class ImmediateRenderContext {

        internal readonly LightList<Vector2> points;
        internal readonly LightList<SVGXStyle> styles;
        internal readonly LightList<SVGXMatrix> transforms;
        internal readonly LightList<SVGXDrawCall> drawCalls;
        internal readonly LightList<SVGXShape> shapes;
        internal readonly LightList<SVGXClipGroup> clipGroups;
        internal readonly Stack<SVGXClipGroup> clipStack;
        
        private Vector2 lastPoint;
        private SVGXMatrix currentMatrix;
        private SVGXStyle currentStyle;
        private RangeInt currentShapeRange;

        public ImmediateRenderContext() {
            points = new LightList<Vector2>(128);
            styles = new LightList<SVGXStyle>();
            transforms = new LightList<SVGXMatrix>();
            currentMatrix = SVGXMatrix.identity;
            drawCalls = new LightList<SVGXDrawCall>();
            shapes = new LightList<SVGXShape>();
            clipGroups = new LightList<SVGXClipGroup>();
            shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
        }

        public void SetStrokeColor(Color color) {
            this.currentStyle.strokeStyle.color = color;
        }

        public void MoveTo(float x, float y) {
            // todo -- if last was move to, set point and return
            lastPoint = currentMatrix.Transform(new Vector2(x, y));
            SVGXShape currentShape = shapes[shapes.Count - 1];
            if (currentShape.type != SVGXShapeType.Unset) {
                shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
                currentShapeRange.length++;
            }
        }

        public void LineTo(float x, float y) {
            SVGXShape currentShape = shapes[shapes.Count - 1];

            Vector2 point = currentMatrix.Transform(new Vector2(x, y));

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
            Vector2 end = currentMatrix.Transform(new Vector2(endX, endY));

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
            ctrl0 = currentMatrix.Transform(ctrl0);
            ctrl1 = currentMatrix.Transform(ctrl1);
            end = currentMatrix.Transform(end);

            int pointStart = points.Count;
            int pointCount = SVGXBezier.CubicCurve(points, lastPoint, ctrl0, ctrl1, end);
            UpdateShape(pointStart, pointCount);
            lastPoint = end;
        }

        public void QuadraticCurveTo(Vector2 ctrl, Vector2 end) {
            ctrl = currentMatrix.Transform(ctrl);
            end = currentMatrix.Transform(end);

            int pointStart = points.Count;
            int pointCount = SVGXBezier.QuadraticCurve(points, lastPoint, ctrl, end);
            UpdateShape(pointStart, pointCount);

            lastPoint = end;
        }

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
            currentStyle = new SVGXStyle();
            currentMatrix = SVGXMatrix.identity;
            lastPoint = Vector2.zero;
            shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
            currentShapeRange = new RangeInt();
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

            int parentId = -1;
            if (clipStack.Count > 0) {
                parentId = clipStack.Peek().id;
            }

            SVGXClipGroup clipGroup = default;//new SVGXClipGroup(clipGroups.Count, parentId, currentShapeRange);
            clipGroups.Add(clipGroup);
        }

        public void PopClip() {
            if (clipStack.Count > 0) {
                clipStack.Pop();
            }
        }

        public void Rect(float x, float y, float width, float height) {
            SimpleShape(SVGXShapeType.Rect, x, y, width, height);
        }

        public void Ellipse(float cx, float cy, float rx, float ry) {
            SimpleShape(SVGXShapeType.Ellipse, cx - rx, cy - ry, rx * 2f, ry * 2f);
        }

        public void Circle(float x, float y, float radius) {
            SimpleShape(SVGXShapeType.Circle, x, y, radius * 2f, radius * 2f);
        }

        private void SimpleShape(SVGXShapeType shapeType, float x, float y, float width, float height) {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            SVGXShapeType lastType = currentShape.type;
            
            Vector2 x0y0 = new Vector2(x, y);
            Vector2 x1y0 = new Vector2(x + width, y);
            Vector2 x1y1 = new Vector2(x + width, y + height);
            Vector2 x0y1 = new Vector2(x, y + height);

            currentShape = new SVGXShape(shapeType, new RangeInt(points.Count, 4));

            points.EnsureAdditionalCapacity(4);
            points.AddUnchecked(x0y0);
            points.AddUnchecked(x1y0);
            points.AddUnchecked(x1y1);
            points.AddUnchecked(x0y1);

            currentShape.isClosed = true;

            if (lastType != SVGXShapeType.Unset) {
                shapes.Add(currentShape);
            }
            else {
                shapes[shapes.Count - 1] = currentShape;
            }

            currentShapeRange.length++;
        }
        
        public void BeginPath() {
            SVGXShape currentShape = shapes[shapes.Count - 1];
            if (currentShape.type != SVGXShapeType.Unset) {
                shapes.Add(new SVGXShape(SVGXShapeType.Unset, default));
                currentShapeRange = new RangeInt(shapes.Count - 1, 0);
            }
        }

        public void Fill() {
            SVGXDrawCall drawCall = new SVGXDrawCall(DrawCallType.StandardFill, currentStyle, currentMatrix, currentShapeRange);
            drawCalls.Add(drawCall);
        }

        public void Stroke() {
            SVGXDrawCall drawCall = new SVGXDrawCall(DrawCallType.StandardStroke, currentStyle, currentMatrix, currentShapeRange);
            drawCalls.Add(drawCall);
        }

        public void SetFillColor(Color color) {
            currentStyle.fillColor = color;
        }

    }

}