using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using static MiProduction.BroAudio.Utility;

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
		/// <param name="firstRatio">�Ĥ@��Rect�����</param>
		/// <param name="gap">��̪����j</param>
		/// <param name="rect1">��X���Ĥ@��Rect</param>
		/// <param name="rect2">��X���ĤG��Recr</param>
		public static void SplitRectHorizontal(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width * firstRatio - halfGap, origin.height);
			rect2 = new Rect(rect1.xMax + gap, origin.y, origin.width * (1 - firstRatio) - halfGap, origin.height);
		}

		public static void SplitRectHorizontal(Rect origin, float[] allRatio ,float gap, out Rect rect1, out Rect rect2,out Rect rect3)
		{
			if(allRatio.Sum() != 1)
			{
				LogError("[Editor] Split ratio's sum should be 1");
				rect1 = default;
				rect2 = default;
				rect3 = default;
				return;
			}

			// �����loop
			rect1 = new Rect(origin.x, origin.y, origin.width * allRatio[0], origin.height);
			rect2 = new Rect(rect1.xMax + gap, origin.y, origin.width * allRatio[1] - gap, origin.height);
			rect3 = new Rect(rect2.xMax + gap, origin.y, origin.width * allRatio[2] - gap, origin.height);
		}

		public static bool TrySplitRectHorizontal(Rect origin, float[] allRatio, float gap, out Rect[] outputRects)
		{
			if (allRatio.Sum() != 1)
			{
				LogError("[Editor] Split ratio's sum should be 1");
				outputRects = null;
				return false;
			}

			Rect[] results = new Rect[allRatio.Length];

			for(int i = 0; i < results.Length;i++)
			{
				float offsetWidth = i == 0 || i == results.Length - 1 ? gap : gap * 0.5f;
				float newWidth = origin.width * allRatio[i] - offsetWidth;
				float newX = i > 0 ? results[i - 1].xMax + gap : origin.x;
				results[i] = new Rect(newX, origin.y, newWidth , origin.height);
			}
			outputRects = results;
			return true;
		}

		public static void SplitRectVertical(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width, origin.height * firstRatio - halfGap);
			rect2 = new Rect(origin.x, rect1.yMax + gap, origin.width, origin.height * (1 - firstRatio) - halfGap);
		}

		/// <summary>
		/// �b��lRect�������w��Ҧ�m��ܷs��Rect(�������lRect��ø�s)
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="dissolveRatio"></param>
		/// <returns></returns>
		public static Rect DissolveHorizontal(this Rect origin,float dissolveRatio)
		{
			return new Rect(origin.xMin + dissolveRatio * origin.width, origin.y, dissolveRatio * origin.width, origin.height);
		}
	}

}