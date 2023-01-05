using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIStyleHelper
{
	public static GUIStyleHelper Instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = new GUIStyleHelper();
			}
			return _instance;
		}
	}

	private static GUIStyleHelper _instance;
	private GUIStyle _richTextStyle;
	private GUIStyle _middleCenterStyle;

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
}
