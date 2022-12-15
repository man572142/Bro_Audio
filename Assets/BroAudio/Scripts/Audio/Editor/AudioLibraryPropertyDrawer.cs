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
		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private float[] FadeValues = new float[2];
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private float[] PlaybackValues = new float[2];

		private bool _isShowClipView = false;

		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get ; set; }

		//protected abstract Vector3[] GetClipLinePoints(float width);
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
				nameProperty.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProperty.stringValue);
				volumeProperty.floatValue = EditorGUI.Slider(GetRectAndIterateLine(position), "Volume", volumeProperty.floatValue, 0f, 1f);
				
				DrawAdditionalBaseProperties(position, property);
				BasePropertiesLineCount = DrawLineCount + 1;

				#region Clip Properties
				EditorGUI.PropertyField(GetRectAndIterateLine(position), property.FindPropertyRelative("Clip"));
				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
				if (clip != null)
				{
					ClipLength = clip.length;
					DrawClipProperites(position, property);
					DrawAdditionalClipProperties(position, property);

					SerializedProperty isShowClipProperty = property.FindPropertyRelative("IsShowClipView");
					isShowClipProperty.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProperty.boolValue, "ClipView");

					ClipPropertiesLineCount = DrawLineCount - BasePropertiesLineCount;

					if (isShowClipProperty.boolValue)
					{
						Rect clipViewRect = GetRectAndIterateLine(position);
						clipViewRect.height = ClipViewHeight;
						Rect waveformRect = new Rect(clipViewRect.xMin + clipViewRect.width * 0.1f, clipViewRect.yMin + clipViewRect.height * 0.1f, clipViewRect.width * 0.9f, clipViewRect.height);

						DrawWaveformPreview(clip, waveformRect);
						DrawPlaybackButton(clip, property.FindPropertyRelative("StartPosition").floatValue, clipViewRect);
						DrawClipPlaybackLine(waveformRect);
					}
				}
				
				#endregion
			}

			EditorGUI.EndProperty();
		}

		private static void DrawWaveformPreview(AudioClip clip, Rect waveformRect)
		{
			Texture2D waveformTexture = AssetPreview.GetAssetPreview(clip);
			if (waveformTexture != null)
			{
				GUI.DrawTexture(waveformRect, waveformTexture);
			}
			EditorGUI.DrawRect(waveformRect, new Color(0.05f, 0.05f, 0.05f, 0.3f));
		}

		private void DrawPlaybackButton(AudioClip clip, float startPos,  Rect clipViewRect)
		{
			float width = Mathf.Clamp(clipViewRect.width * 0.08f, clipViewRect.width * 0.08f,clipViewRect.height *0.4f);
			float height = Mathf.Clamp(width,width,clipViewRect.height * 0.4f);
			Rect playRect = new Rect(clipViewRect.xMin, clipViewRect.yMin + ClipViewHeight * 0.15f, width,height);
			Rect stopRect = new Rect(clipViewRect.xMin, clipViewRect.yMin + ClipViewHeight * 0.65f, width,height);
			if (GUI.Button(playRect, "▶"))
			{
				EditorPlayAudioClip.StopAllClips();
				EditorPlayAudioClip.PlayClip(clip, Mathf.RoundToInt(AudioSettings.outputSampleRate * startPos));
			}
			if (GUI.Button(stopRect, "■"))
			{
				EditorPlayAudioClip.StopAllClips();
			}

			EditorGUI.DrawRect(playRect, new Color(0.25f, 0.9f, 0.25f, 0.4f));
			EditorGUI.DrawRect(stopRect, new Color(0.9f, 0.25f, 0.25f, 0.4f));
		}

		private void DrawClipPlaybackLine(Rect waveformRect)
		{
			Vector3[] points = GetClipLinePoints(waveformRect.width);
			if (points.Length < 4)
			{
				return;
			}

			GUI.BeginClip(waveformRect);
			Handles.color = Color.green;
			Handles.DrawAAPolyLine(2f, points);

			Handles.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
			Vector3[] leftBlock = new Vector3[4];
			leftBlock[0] = Vector3.zero;
			leftBlock[1] = points[1];
			leftBlock[2] = points[0];
			leftBlock[3] = new Vector3(0f, ClipViewHeight, 0f);
			Handles.DrawAAConvexPolygon(leftBlock);

			Vector3[] rightBlock = new Vector3[4];
			rightBlock[0] = points[2];
			rightBlock[1] = new Vector3(waveformRect.width, 0f, 0f);
			rightBlock[2] = new Vector3(waveformRect.width, ClipViewHeight, 0f);
			rightBlock[3] = points[3];
			Handles.DrawAAConvexPolygon(rightBlock);

			GUI.EndClip();
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

		protected Vector3[] GetClipLinePoints(float width)
		{
			if (width <= 0f)
				return new Vector3[0];

			Vector3[] points = new Vector3[4];
			// Start
			points[0] = new Vector3(Mathf.Lerp(0f, width, PlaybackValues[0] / ClipLength), ClipViewHeight, 0f);
			// FadeIn
			points[1] = new Vector3(Mathf.Lerp(0f, width, (PlaybackValues[0] + FadeValues[0]) / ClipLength), 0f, 0f);
			// FadeOut
			points[2] = new Vector3(Mathf.Lerp(0f, width, (ClipLength - PlaybackValues[1] - FadeValues[1]) / ClipLength), 0f, 0f);
			// End
			points[3] = new Vector3(Mathf.Lerp(0f, width, (ClipLength - PlaybackValues[1]) / ClipLength), ClipViewHeight, 0f);

			return points;
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = property.isExpanded ? SingleLineSpace * BasePropertiesLineCount : SingleLineSpace;
			if (property.isExpanded)
			{
				AudioClip clip = property.FindPropertyRelative("Clip").objectReferenceValue as AudioClip;
				bool isShowClipView = property.FindPropertyRelative("IsShowClipView").boolValue;
				if(clip != null)
				{
					height += SingleLineSpace * ClipPropertiesLineCount;
				}
				if(isShowClipView)
				{
					height += SingleLineSpace + ClipViewHeight;
				}
				//height += clip != null ? ClipViewHeight + SingleLineSpace * (ClipPropertiesLineCount +1) : 0f;
			}

			return height;
		}

		private float GetLengthLimit(float start, float end, float fadeIn, float fadeOut)
		{

			return ClipLength - start - end - fadeIn - fadeOut;
		}




	}

}