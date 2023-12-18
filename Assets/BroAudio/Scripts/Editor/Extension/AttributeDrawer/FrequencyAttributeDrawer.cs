using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.Extension.EditorScriptingExtension;
using System;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(Frequency))]
	public class FrequencyAttributeDrawer : PropertyDrawer
	{
		public const int Digits = 1;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(property.propertyType == SerializedPropertyType.Float)
			{
				Rect suffixRect = EditorGUI.PrefixLabel(position, label);

				float freq = DrawLogarithmicSlider_Horizontal(suffixRect, property.floatValue, AudioConstant.MinFrequency, AudioConstant.MaxFrequency);
				property.floatValue = (float)Math.Floor(freq);
			}
			else
			{
				base.OnGUI(position, property, label);
			}
		}
	} 
}
