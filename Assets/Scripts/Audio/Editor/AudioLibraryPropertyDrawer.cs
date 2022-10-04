using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

namespace MiProduction.BroAudio.Library
{
	[CustomPropertyDrawer(typeof(SoundLibrary))]
	public class AudioLibraryPropertyDrawer : PropertyDrawer
	{
		protected const float ClipViewHeight = 100f;

		protected Dictionary<string, (bool isFold, bool hasClip)> _elementState = new Dictionary<string, (bool isFold, bool hasClip)>();


		public float SingleLineSpace
		{
			get => EditorGUIUtility.singleLineHeight + 3f;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			Rect nameRect = new Rect(position.x, position.y + SingleLineSpace, position.width, EditorGUIUtility.singleLineHeight);
			Rect enumRect = new Rect(position.x, position.y + SingleLineSpace * 2, position.width, EditorGUIUtility.singleLineHeight);
			Rect volRect = new Rect(position.x, position.y + SingleLineSpace * 3, position.width, EditorGUIUtility.singleLineHeight);
			Rect clipAssetRect = new Rect(position.x, position.y + SingleLineSpace * 4, position.width, EditorGUIUtility.singleLineHeight);
			Rect startPosRect = new Rect(position.x, position.y + SingleLineSpace * 5, position.width, EditorGUIUtility.singleLineHeight);
			// startPos會在沒Clip的時候也增加Property高度 (待解決)
			Rect clipViewRect = new Rect(position.x, position.y + SingleLineSpace * 6, position.width, ClipViewHeight);
			Rect waveformRect = new Rect(position.x, position.y + SingleLineSpace * 6, position.width, ClipViewHeight);
			Rect totalRect = new Rect(position.x, position.y, position.width, position.height + SingleLineSpace * 6);

			EditorGUI.BeginProperty(totalRect, label, property);

			SerializedProperty nameProperty = property.FindPropertyRelative("Name");
			SerializedProperty volumeProperty = property.FindPropertyRelative("Volume");
			SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");


			bool isFoldArray = false;
			bool hasClip = false;
			if (!_elementState.ContainsKey(nameProperty.stringValue))
			{
				_elementState.Add(nameProperty.stringValue, (isFoldArray, hasClip));
			}
			(bool isFold, bool hasClip) state = _elementState[nameProperty.stringValue];

			state.isFold = EditorGUI.Foldout(foldRect, state.isFold, nameProperty.stringValue);
			if (state.isFold)
			{
				nameProperty.stringValue = EditorGUI.TextField(nameRect, "Name", nameProperty.stringValue);
				EditorGUI.PropertyField(enumRect, property.FindPropertyRelative("Sound"));
				volumeProperty.floatValue = EditorGUI.Slider(volRect, "Volume", volumeProperty.floatValue, 0f, 1f);
				EditorGUI.PropertyField(clipAssetRect, property.FindPropertyRelative("Clip"));
				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;

				if (clip != null)
				{
					startPosProperty.floatValue = Mathf.Clamp(EditorGUI.FloatField(startPosRect, "Start Position", startPosProperty.floatValue), 0f, clip.length);
					Texture2D waveformTexture = AssetPreview.GetAssetPreview(clip);
					if (waveformTexture != null)
					{
						GUI.DrawTexture(waveformRect, waveformTexture);
					}
					EditorGUI.DrawRect(clipViewRect, new Color(0.05f, 0.05f, 0.05f, 0.3f));

					float startX = (startPosProperty.floatValue / clip.length) * position.width;
					GUI.BeginClip(clipViewRect);
					Handles.color = Color.green;
					Handles.DrawAAPolyLine(3f, new Vector3(startX, ClipViewHeight, 0f), new Vector3(startX, 0f, 0f));
					GUI.EndClip();
				}
				state.hasClip = clip != null;
			}
			_elementState[nameProperty.stringValue] = state;

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			string propertyName = property.FindPropertyRelative("Name").stringValue;

			float foldHeight = 0f;
			float clipHeight = 0f;
			if (_elementState.TryGetValue(propertyName, out var state))
			{
				foldHeight = state.isFold ? SingleLineSpace * 6 : 0f;
				clipHeight = state.hasClip && state.isFold ? ClipViewHeight : 0f;
			}
			return foldHeight + clipHeight + EditorGUIUtility.singleLineHeight;
		}
	}

}