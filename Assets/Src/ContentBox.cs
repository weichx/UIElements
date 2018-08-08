﻿using UnityEngine;

public struct ContextBoxPart {

    public float top;
    public float right;
    public float bottom;
    public float left;

    public ContextBoxPart(float top, float right, float bottom, float left) {
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.left = left;
    }

    public float horizontal => right + left;
    public float vertical => top     + bottom;

}

public enum FitType {

    None,
    Content,
    Parent

}

public class ContentBox {

    public ContextBoxPart border;
    public ContextBoxPart margin;
    public ContextBoxPart padding;
    
    public FitType widthFit;
    public FitType heightFit;
    
    public float totalWidth;
    public float totalHeight;

    public float GetContentWidth() {
        return Mathf.Min(0, totalWidth - (border.horizontal + margin.horizontal + padding.horizontal));
    }

    public float GetContentHeight() {
        return Mathf.Min(0, totalHeight - (border.vertical + margin.vertical + padding.vertical));
    }

    public float SetContentWidth(float width) {
        totalWidth = width - (border.horizontal + margin.horizontal + padding.horizontal);
        return totalWidth;
    }

    public float SetContentHeight(float height) {
        totalHeight = height - (border.vertical + margin.vertical + padding.vertical);
        return totalHeight;
    }

}