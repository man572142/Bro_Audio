using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EditorDrawingUtility
{
    public static Rect GetRectAndIterateLine(IEditorDrawer drawer, Rect position)
    {
        Rect newRect = new Rect(position.x, position.y + drawer.SingleLineSpace * drawer.LineIndex, position.width, EditorGUIUtility.singleLineHeight);
        drawer.LineIndex++;

        return newRect;
    }
}
