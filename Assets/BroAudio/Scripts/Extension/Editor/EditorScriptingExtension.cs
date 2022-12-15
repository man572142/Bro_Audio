using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.Extension
{
	public static class EditorScriptingExtension
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

		/// <summary>
		/// �NRect�̫��w��Ҥ�����������Rect
		/// </summary>
		/// <param name="origin">��lRect</param>
		/// <param name="firstRectRatio">�Ĥ@��Rect�����</param>
		/// <param name="gap">��̪����j</param>
		/// <param name="rect1">��X���Ĥ@��Rect</param>
		/// <param name="rect2">��X���ĤG��Recr</param>
		public static void SplitRectHorizontal(Rect origin, float firstRectRatio, float gap, out Rect rect1, out Rect rect2)
		{
			rect1 = new Rect(origin.x, origin.y, origin.width * firstRectRatio, origin.height);
			rect2 = new Rect(rect1.x + rect1.width + gap, origin.y, origin.width - rect1.width - gap, origin.height);
		}
	}

}