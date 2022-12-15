using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.Extension
{
	public static class EditorScriptingExtension
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

		/// <summary>
		/// 將Rect依指定比例水平拆分為兩個Rect
		/// </summary>
		/// <param name="origin">原始Rect</param>
		/// <param name="firstRectRatio">第一個Rect的比例</param>
		/// <param name="gap">兩者的間隔</param>
		/// <param name="rect1">輸出的第一個Rect</param>
		/// <param name="rect2">輸出的第二個Recr</param>
		public static void SplitRectHorizontal(Rect origin, float firstRectRatio, float gap, out Rect rect1, out Rect rect2)
		{
			rect1 = new Rect(origin.x, origin.y, origin.width * firstRectRatio, origin.height);
			rect2 = new Rect(rect1.x + rect1.width + gap, origin.y, origin.width - rect1.width - gap, origin.height);
		}
	}

}