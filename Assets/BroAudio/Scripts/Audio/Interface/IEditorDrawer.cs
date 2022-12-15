using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEditorDrawer
{
    public float SingleLineSpace { get; }
    public int DrawLineCount { get; set;}
    public Rect GetRectAndIterateLine(Rect position);
}
