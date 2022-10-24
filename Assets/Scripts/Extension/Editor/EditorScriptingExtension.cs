using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EditorDrawingUtility
{
    /// <summary>
    /// ���o�ثeø�s�����@�檺Rect�A�����۰ʭ��N�ܤU�� (���涶�ǱN�|�M�wø�s����m)
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
