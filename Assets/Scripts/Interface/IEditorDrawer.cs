using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditorDrawer
{
    // 其實有點多餘...?
    public float SingleLineSpace { get; }
    public int DrawLineCount { get; set;}
    public Rect GetRectAndIterateLine(Rect position);
}
