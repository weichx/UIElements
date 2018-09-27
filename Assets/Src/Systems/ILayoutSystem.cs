﻿using System;
using System.Collections.Generic;
using Rendering;
using Src.Elements;
using UnityEngine;

namespace Src.Systems {

    public interface ILayoutSystem : ISystem {

        event Action<VirtualScrollbar> onCreateVirtualScrollbar;
        event Action<VirtualScrollbar> onDestroyVirtualScrollbar;

        void SetViewportRect(Rect viewportRect);

        List<UIElement> QueryPoint(Vector2 point, List<UIElement> retn);
        List<LayoutResult> GetLayoutResults(List<LayoutResult> retn);

    }

}