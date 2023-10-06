using UnityEditor;
using UnityEngine;
using static Ami.Extension.EditorVersionAdapter;

namespace Ami.Extension
{
	public static class GUIStyleHelper
	{
		public static Color DefaultLabelColor => GUI.skin.label.normal.textColor;

		public static GUIStyle MiddleCenterText
		{
			get
			{
				GUIStyle style = new GUIStyle();
				style.alignment = TextAnchor.MiddleCenter;
				style.normal.textColor = Color.white;
				return style;
			}
		}

		public static GUIStyle RichText
		{
			get
			{
				GUIStyle style = new GUIStyle();
				style.richText = true;
				return style;
			}
		}

		public static GUIStyle MiddleCenterRichText
		{
			get
			{
				GUIStyle style = new GUIStyle(MiddleCenterText);
				style.richText = true;
				return style;
			}
		}

		public static GUIStyle RichTextHelpBox
		{
			get
			{
				GUIStyle style = new GUIStyle(EditorStyles.helpBox);
				style.richText = true;
				return style;
			}
		}

		public static GUIStyle LinkLabelStyle
		{
			get
			{
				GUIStyle style = new GUIStyle(LinkLabel);
				style.alignment = TextAnchor.MiddleCenter;
				return style;
			}
		}
	}
}