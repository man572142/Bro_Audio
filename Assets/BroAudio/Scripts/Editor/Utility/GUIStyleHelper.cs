using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.Extension
{
	public class GUIStyleHelper
	{
		public static GUIStyleHelper Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new GUIStyleHelper();
				}
				return _instance;
			}
		}

		private static GUIStyleHelper _instance;
		private GUIStyle _richTextStyle;
		private GUIStyle _middleCenterStyle;
		private GUIStyle _defaultDarkBackground;
		private GUIStyle _fieldStyle;
		private GUIStyle _richTextHelpbox;
		private GUIStyle _upperCenterStyle;
		private GUIStyle _middleRightStyle;

		public GUIStyle MiddleCenterText
		{
			get
			{
				if (_middleCenterStyle == null)
				{
					_middleCenterStyle = new GUIStyle();
					_middleCenterStyle.alignment = TextAnchor.MiddleCenter;
					_middleCenterStyle.normal.textColor = Color.white;
				}
				return _middleCenterStyle;
			}
		}

		public GUIStyle MiddleRightText
		{
			get
			{
				if (_middleRightStyle == null)
				{
					_middleRightStyle = new GUIStyle(MiddleCenterText);
					_middleRightStyle.alignment = TextAnchor.MiddleRight;
				}
				return _middleRightStyle;
			}
		}

		public GUIStyle RichText
		{
			get
			{
				if (_richTextStyle == null)
				{
					_richTextStyle = new GUIStyle();
					_richTextStyle.richText = true;
				}
				return _richTextStyle;
			}
		}

		public GUIStyle MiddleCenterRichText
		{
			get
			{
				GUIStyle result = new GUIStyle(MiddleCenterText);
				result.richText = true;
				return result;
			}
		}

		public GUIStyle DefaultDarkBackground
		{
			get
			{
				if (_defaultDarkBackground == null)
				{
					_defaultDarkBackground = new GUIStyle(GUI.skin.box);
				}
				return _defaultDarkBackground;
			}
		}

		public GUIStyle DefaultFieldStyle
		{
			get
			{
				if (_fieldStyle == null)
				{
					_fieldStyle = new GUIStyle(EditorStyles.textField);
				}
				return _fieldStyle;
			}
		}

		public GUIStyle RichTextHelpBox
		{
			get
			{
				if (_richTextHelpbox == null)
				{
					_richTextHelpbox = new GUIStyle(EditorStyles.helpBox);
					_richTextHelpbox.richText = true;
				}
				return _richTextHelpbox;
			}
		}

		public GUIStyle UpperCenterStyle
		{
			get
			{
				if(_upperCenterStyle == null)
				{
					_upperCenterStyle = new GUIStyle();
					_upperCenterStyle.alignment = TextAnchor.UpperCenter;
				}
				return _upperCenterStyle;
			}
		}
	}

}