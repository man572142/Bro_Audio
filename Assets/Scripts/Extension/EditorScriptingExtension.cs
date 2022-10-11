using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EditorDrawingUtility
{
    /// <summary>
    /// 取得目前繪製的那一行的Rect，取完自動迭代至下行 (執行順序將會決定繪製的位置)
    /// </summary>
    /// <param name="drawer"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Rect GetRectAndIterateLine(IEditorDrawer drawer, Rect position)
    {
        Rect newRect = new Rect(position.x, position.y + drawer.SingleLineSpace * drawer.DrawLineCount, position.width, EditorGUIUtility.singleLineHeight);
        drawer.DrawLineCount++;

        return newRect;
    }
}
