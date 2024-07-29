using UnityEditor;
using UnityEngine;

namespace Ami.Extension
{
	public static class GUIStyleHelper
	{
		public static Color DefaultLabelColor => GUI.skin.label.normal.textColor;
		public static Color LinkBlue => LinkLabelStyle.normal.textColor;

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
				style.alignment = TextAnchor.MiddleLeft;
				style.normal.textColor = DefaultLabelColor;
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
				GUIStyle style = new GUIStyle(EditorStyles.linkLabel);
				return style;
			}
		}
	}
}