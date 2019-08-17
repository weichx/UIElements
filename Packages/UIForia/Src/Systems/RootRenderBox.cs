using UIForia.Rendering;
using UnityEngine;

namespace Src.Systems {

    public class RootRenderBox : RenderBox {

        public override Rect RenderBounds => new Rect(0, 0, element.View.Viewport.width, element.View.Viewport.height);

        public override void PaintBackground(RenderContext ctx) { }

    }

}