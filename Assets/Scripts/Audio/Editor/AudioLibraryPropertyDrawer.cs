using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

namespace MiProduction.BroAudio.Library
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer
	{
		protected const float ClipViewHeight = 100f;

		// TODO: 如果有同名的會一起開關，需要優化
		protected Dictionary<string, (bool isFold, bool hasClip)> _elementState = new Dictionary<string, (bool isFold, bool hasClip)>();

		protected int LineIndex = 0;
		protected int BasePropertiesLineCount = 0;
		protected int ClipPropertiesLineCount = 0;

		protected abstract Vector3[] GetClipLinePoints(float width);
		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawClipProperties(Rect position, SerializedProperty property,float clipLength);

		public float SingleLineSpace
		{
			get => EditorGUIUtility.singleLineHeight + 3f;
		}

		/// <summary>
		/// 取得目前繪製的那一行的Rect，取完自動迭代至下行 (執行順序將會決定繪製的位置)
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		protected Rect GetRectAndIterateLine(Rect position)
		{
			Rect newRect = new Rect(position.x, position.y + SingleLineSpace * LineIndex, position.width, EditorGUIUtility.singleLineHeight);
			LineIndex++;

			return newRect;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			LineIndex = 0;
			//Rect totalRect = new Rect(position.x, position.y, position.width, position.height + SingleLineSpace * (LineIndex + 1));

			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty nameProperty = property.FindPropertyRelative("Name");
			SerializedProperty volumeProperty = property.FindPropertyRelative("Volume");

            bool isFoldArray = false;
			bool hasClip = false;
			if (!_elementState.ContainsKey(nameProperty.stringValue))
			{
				_elementState.Add(nameProperty.stringValue, (isFoldArray, hasClip));
			}
			(bool isFold, bool hasClip) state = _elementState[nameProperty.stringValue];

			state.isFold = EditorGUI.Foldout(GetRectAndIterateLine(position), state.isFold, nameProperty.stringValue);
			if (state.isFold)
			{
				// Name
				nameProperty.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProperty.stringValue);
				// Volume
				volumeProperty.floatValue = EditorGUI.Slider(GetRectAndIterateLine(position), "Volume", volumeProperty.floatValue, 0f, 1f);
				
				DrawAdditionalBaseProperties(position, property);
				BasePropertiesLineCount = LineIndex + 1;

				// Clip Asset
				EditorGUI.PropertyField(GetRectAndIterateLine(position), property.FindPropertyRelative("Clip"));

				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
				if (clip != null)
				{
					

                    DrawClipProperties(position, property,clip.length);
					
					ClipPropertiesLineCount = LineIndex - BasePropertiesLineCount ;


					Rect clipViewRect = GetRectAndIterateLine(position);
					Rect waveformRect = new Rect(clipViewRect.xMin + clipViewRect.width *0.1f,clipViewRect.center.y ,clipViewRect.width * 0.9f, ClipViewHeight);
					Rect playRect = new Rect(clipViewRect.xMin,clipViewRect.yMin + ClipViewHeight * 0.15f, 40f, 40f);
					Rect stopRect = new Rect(clipViewRect.xMin,clipViewRect.yMin + ClipViewHeight * 0.65f, 40f, 40f);


					if (GUI.Button(playRect, "▶"))
					{
						// 還要抓START POS
						EditorPlayAudioClip.PlayClip(clip);
					}
					if (GUI.Button(stopRect, "■"))
					{
						EditorPlayAudioClip.StopAllClips();
					}
					EditorGUI.DrawRect(playRect, new Color(0.25f, 0.9f, 0.25f, 0.4f));
					EditorGUI.DrawRect(stopRect, new Color(0.9f, 0.25f, 0.25f, 0.4f));
					#region Draw Waveform
					Texture2D waveformTexture = AssetPreview.GetAssetPreview(clip);
					//Rect waveformRect = new Rect(position.x, position.y + SingleLineSpace * (LineIndex + 1), position.width, ClipViewHeight);
					if (waveformTexture != null)
					{
						GUI.DrawTexture(waveformRect, waveformTexture);
					}
					EditorGUI.DrawRect(waveformRect, new Color(0.05f, 0.05f, 0.05f, 0.3f));

					GUI.BeginClip(waveformRect);
					Handles.color = Color.green;
					Handles.DrawAAPolyLine(2f, GetClipLinePoints(waveformRect.width));
					GUI.EndClip();
					#endregion
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
				foldHeight = state.isFold ? 
					SingleLineSpace * BasePropertiesLineCount : 0f;
				clipHeight = state.hasClip && state.isFold ? 
					ClipViewHeight + SingleLineSpace * ClipPropertiesLineCount  : 0f;
			}

			//Debug.Log($"FoldHeight:{foldHeight}, ClipHeight:{clipHeight} , LineIndexBeforeClip:{BasePropertiesLineCount} ");

			return foldHeight + clipHeight + EditorGUIUtility.singleLineHeight;
		}

		
		

	}

}