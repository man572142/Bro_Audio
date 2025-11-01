using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Ami.Extension
{
	[CustomPropertyDrawer(typeof(ReadOnlyTextArea))]
	public class ReadOnlyTextAreaAttributeDrawer : MiPropertyDrawer
	{
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(position, property, label);

			if(property.propertyType == SerializedPropertyType.String)
			{
				ReadOnlyTextArea targetAttribute = attribute as ReadOnlyTextArea;

				EditorGUI.LabelField(GetRectAndIterateLine(position), label);

				Rect textAreaRect = GetRectAndIterateLine(position);
				textAreaRect.height = EditorGUIUtility.singleLineHeight * GetLinesCount(CalcHeight(property.stringValue));

				EditorGUI.BeginDisabledGroup(targetAttribute.ReadOnly);
				{
					GUIStyle areaStyle = new GUIStyle(EditorStyles.textArea);
					areaStyle.wordWrap = true;
					property.stringValue = EditorGUI.TextArea(textAreaRect, property.stringValue, areaStyle);
				}
				EditorGUI.EndDisabledGroup();	
			}
			else
			{
				Debug.LogError($"{nameof(ReadOnlyTextArea)} could only be used for string field!");
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			int textLines = GetLinesCount(CalcHeight(property.stringValue));
			return EditorGUIUtility.singleLineHeight * (textLines + 1);
		}

		private int GetLinesCount(float fullTextHeight)
		{
			return (int)Math.Ceiling(fullTextHeight / EditorGUIUtility.singleLineHeight);
		}

		private float CalcHeight(string text)
		{
			return EditorStyles.textArea.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth);
		}
	}
}