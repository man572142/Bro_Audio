using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace MiProduction.BroAudio.Library
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer,IEditorDrawer
	{
		protected const float ClipViewHeight = 100f;
		protected int BasePropertiesLineCount = 0;
		protected int ClipPropertiesLineCount = 0;
		protected float ClipLength = 0f;
		protected GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		protected float[] FadeValues = new float[2];
		protected GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		protected float[] PlaybackValues = new float[2];
		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get ; set; }

		protected abstract Vector3[] GetClipLinePoints(float width);
		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawAdditionalClipProperties(Rect position, SerializedProperty property);

		public Rect GetRectAndIterateLine(Rect position)
		{
			return EditorDrawingUtility.GetRectAndIterateLine(this, position);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DrawLineCount = 0;

			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty nameProperty = property.FindPropertyRelative("Name");
			SerializedProperty volumeProperty = property.FindPropertyRelative("Volume");

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProperty.stringValue);
			if (property.isExpanded)
			{
				// Name
				nameProperty.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProperty.stringValue);
				// Volume
				volumeProperty.floatValue = EditorGUI.Slider(GetRectAndIterateLine(position), "Volume", volumeProperty.floatValue, 0f, 1f);
				
				DrawAdditionalBaseProperties(position, property);
				BasePropertiesLineCount = DrawLineCount + 1;

				// Clip Asset
				EditorGUI.PropertyField(GetRectAndIterateLine(position), property.FindPropertyRelative("Clip"));

				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
				if (clip != null)
				{
					ClipLength = clip.length;
					DrawClipProperites(position,property);
                    DrawAdditionalClipProperties(position, property);
					
					ClipPropertiesLineCount = DrawLineCount - BasePropertiesLineCount ;

					Rect clipViewRect = GetRectAndIterateLine(position);
					Rect waveformRect = new Rect(clipViewRect.xMin + clipViewRect.width *0.1f,clipViewRect.center.y ,clipViewRect.width * 0.9f, ClipViewHeight);
					Rect playRect = new Rect(clipViewRect.xMin,clipViewRect.yMin + ClipViewHeight * 0.15f, 40f, 40f);
					Rect stopRect = new Rect(clipViewRect.xMin,clipViewRect.yMin + ClipViewHeight * 0.65f, 40f, 40f);

					if (GUI.Button(playRect, "▶"))
					{
						SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");

						EditorPlayAudioClip.PlayClip(clip, Mathf.RoundToInt(AudioSettings.outputSampleRate * startPosProperty.floatValue));
					}
					if (GUI.Button(stopRect, "■"))
					{
						EditorPlayAudioClip.StopAllClips();
					}
					EditorGUI.DrawRect(playRect, new Color(0.25f, 0.9f, 0.25f, 0.4f));
					EditorGUI.DrawRect(stopRect, new Color(0.9f, 0.25f, 0.25f, 0.4f));
					#region Draw Waveform
					Texture2D waveformTexture = AssetPreview.GetAssetPreview(clip);
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
			}

			EditorGUI.EndProperty();
		}

		private void DrawClipProperites(Rect position, SerializedProperty property)
		{
			SerializedProperty startPosProperty = property.FindPropertyRelative("StartPosition");
			SerializedProperty endPosProperty = property.FindPropertyRelative("EndPosition");
			SerializedProperty fadeInProperty = property.FindPropertyRelative("FadeIn");
			SerializedProperty fadeOutProperty = property.FindPropertyRelative("FadeOut");

			PlaybackValues[0] = startPosProperty.floatValue;
			PlaybackValues[1] = endPosProperty.floatValue;
			FadeValues[0] = fadeInProperty.floatValue;
			FadeValues[1] = fadeOutProperty.floatValue;

			EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Playback Position"), PlaybackLabels, PlaybackValues);
			startPosProperty.floatValue = Mathf.Clamp(PlaybackValues[0], 0f, GetLengthLimit(0, endPosProperty.floatValue, fadeInProperty.floatValue, fadeOutProperty.floatValue));
			PlaybackValues[0] = startPosProperty.floatValue;
			endPosProperty.floatValue = Mathf.Clamp(PlaybackValues[1], 0f, GetLengthLimit(startPosProperty.floatValue, 0f, fadeInProperty.floatValue, fadeOutProperty.floatValue));
			PlaybackValues[1] = endPosProperty.floatValue;

			EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Fade"), FadeLabels, FadeValues);
			fadeInProperty.floatValue = Mathf.Clamp(FadeValues[0], 0f, GetLengthLimit(startPosProperty.floatValue, endPosProperty.floatValue, 0f, fadeOutProperty.floatValue));
			FadeValues[0] = fadeInProperty.floatValue;
			fadeOutProperty.floatValue = Mathf.Clamp(FadeValues[1], 0f, GetLengthLimit(startPosProperty.floatValue, endPosProperty.floatValue, fadeInProperty.floatValue, 0f));
			FadeValues[1] = fadeOutProperty.floatValue;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = property.isExpanded ? SingleLineSpace * BasePropertiesLineCount : SingleLineSpace;
			if (property.isExpanded)
			{
				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
				height += clip != null ? ClipViewHeight + SingleLineSpace * (ClipPropertiesLineCount +1) : 0f;
			}

			return height;
		}

		private float GetLengthLimit(float start, float end, float fadeIn, float fadeOut)
		{

			return ClipLength - start - end - fadeIn - fadeOut;
		}




	}

}