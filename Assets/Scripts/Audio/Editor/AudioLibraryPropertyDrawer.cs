using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class AudioLibraryPropertyDrawer : PropertyDrawer
	{
		private bool isFoldArray = false;
		private SerializedProperty mainProperty;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect nameRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height);
			Rect enumRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight *2, position.width, position.height);
			Rect clipRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight *3, position.width, position.height);
			Rect volRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight *4, position.width, position.height);
			Rect totalRect = new Rect(position.x, position.y, position.width, position.height + EditorGUIUtility.singleLineHeight * 5);

			EditorGUI.BeginProperty(totalRect, label, property);

			string name = property.FindPropertyRelative("Name").stringValue;
			name = EditorGUI.TextField(nameRect, "Name", name);

			EditorGUI.PropertyField(enumRect, property.FindPropertyRelative("Sound"));

			EditorGUI.PropertyField(clipRect, property.FindPropertyRelative("Clip"));

			float vol = property.FindPropertyRelative("Volume").floatValue;
			vol = EditorGUI.Slider(volRect, "Volume", vol, 0f, 1f);

			EditorGUI.EndProperty();
			Debug.Log(position.ToString());
			//base.OnGUI(position, property, label);
			
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label);
		}
	}

}