using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(Volume))]
	public class VolumeAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(property.propertyType == SerializedPropertyType.Float && attribute is Volume volAttr)
			{
				if(volAttr.CanBoost)
				{
					//Rect suffixRect = EditorGUI.PrefixLabel(position, label);
					property.floatValue = BroEditorUtility.DrawVolumeSlider(position, label, property.floatValue);
				}
				else
				{
					property.floatValue = EditorGUI.Slider(position, label, property.floatValue, 0f, AudioConstant.FullVolume);
				}
				
			}
			else
			{
				EditorGUI.PropertyField(position, property, label);
			}
		}
	} 
}
