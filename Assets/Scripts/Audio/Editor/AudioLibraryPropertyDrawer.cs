using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MiProduction.BroAudio.Library
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer,IEditorDrawer
	{
		protected const float ClipViewHeight = 100f;
		protected int BasePropertiesLineCount = 0;
		protected int ClipPropertiesLineCount = 0;

		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get ; set; }

		protected abstract Vector3[] GetClipLinePoints(float width);
		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawClipProperties(Rect position, SerializedProperty property,float clipLength);

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

            bool isFoldArray = false;
			bool hasClip = false;


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
					

                    DrawClipProperties(position, property,clip.length);
					
					ClipPropertiesLineCount = DrawLineCount - BasePropertiesLineCount ;


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

		




	}

}