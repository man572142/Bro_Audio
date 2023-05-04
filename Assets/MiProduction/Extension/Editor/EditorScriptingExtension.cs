using UnityEditor;
using UnityEngine;
using System.Linq;

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
		public static Rect GetRectAndIterateLine(IEditorDrawLineCounter drawer, Rect position)
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

		/// <summary>
		/// �NRect�̫��w��Ҥ�������������ƶq��Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="allRatio"></param>
		/// <param name="gap"></param>
		/// <param name="outputRects"></param>
		/// <returns></returns>
		public static bool TrySplitRectHorizontal(Rect origin, float[] allRatio, float gap, out Rect[] outputRects)
		{
			if (allRatio.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
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

		/// <summary>
		/// �NRect�̫��w��ҫ�����������Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="firstRatio"></param>
		/// <param name="gap"></param>
		/// <param name="rect1"></param>
		/// <param name="rect2"></param>
		public static void SplitRectVertical(Rect origin, float firstRatio, float gap, out Rect rect1, out Rect rect2)
		{
			float halfGap = gap * 0.5f;
			rect1 = new Rect(origin.x, origin.y, origin.width, origin.height * firstRatio - halfGap);
			rect2 = new Rect(origin.x, rect1.yMax + gap, origin.width, origin.height * (1 - firstRatio) - halfGap);
		}

		/// <summary>
		/// �NRect�̫��w��Ҥ�������������ƶq��Rect
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="allRatio"></param>
		/// <param name="gap"></param>
		/// <param name="outputRects"></param>
		/// <returns></returns>
		public static bool TrySplitRectVertical(Rect origin, float[] allRatio, float gap, out Rect[] outputRects)
		{
			if (allRatio.Sum() != 1)
			{
				Debug.LogError("[Editor] Split ratio's sum should be 1");
				outputRects = null;
				return false;
			}

			Rect[] results = new Rect[allRatio.Length];

			for (int i = 0; i < results.Length; i++)
			{
				float offsetHeight = i == 0 || i == results.Length - 1 ? gap : gap * 0.5f;
				float newHeight = origin.height * allRatio[i] - offsetHeight;
				float newY = i > 0 ? results[i - 1].yMax + gap : origin.y;
				results[i] = new Rect(origin.x,newY, origin.width,newHeight);
			}
			outputRects = results;
			return true;
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

		public static bool TryGetPropertyObject<T>(this SerializedProperty sourceProperty, string propertyPath, out T newProperty) where T : class
		{
			newProperty = null;
			if (sourceProperty == null)
			{
				return false;
			}

			newProperty = sourceProperty.FindPropertyRelative(propertyPath)?.objectReferenceValue as T;
			return newProperty != null;
		}

		/// <summary>
		/// �䴩RichText��HelpBox
		/// </summary>
		/// <param name="message">�T�����e</param>
		/// <param name="messageType">�T������</param>
		public static void RichTextHelpBox(string message, MessageType messageType)
		{
			string icon = string.Empty;
			switch (messageType)
			{
				case MessageType.Info:
					icon = "d_console.infoicon";
					break;
				case MessageType.Warning:
					icon = "d_console.warnicon";
					break;
				case MessageType.Error:
					icon = "d_console.erroricon";
					break;
				default:
					icon = string.Empty;
					break;
			}

			RichTextHelpBox(message, icon);
		}

		/// <summary>
		/// �䴩RichText�Φۭqicon��HelpBox
		/// </summary>
		/// <param name="message">�T�����e</param>
		/// <param name="icon">Unity����Icon�W��</param>
		public static void RichTextHelpBox(string message, string icon)
		{
			GUIStyle richTextHelpBox = new GUIStyle(EditorStyles.helpBox);
			richTextHelpBox.richText = true;

			GUIContent content = string.IsNullOrEmpty(icon)? new GUIContent(message) : new GUIContent(message, EditorGUIUtility.IconContent(icon).image);
			EditorGUILayout.LabelField(content, richTextHelpBox);
		}

		/// <summary>
		/// ���oProperty�۰ʥͦ�BackingField���W��
		/// </summary>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		public static string GetBackingFieldName(string propertyName)
		{
			return $"<{propertyName}>k__BackingField";
		}
	}
}
