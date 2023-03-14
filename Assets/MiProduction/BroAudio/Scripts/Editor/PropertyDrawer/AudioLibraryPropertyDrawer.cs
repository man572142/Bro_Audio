using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using MiProduction.Extension;
using static MiProduction.Extension.EditorScriptingExtension;
using UnityEngine.UIElements;
using System;

namespace MiProduction.BroAudio.Library.Core
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer, IEditorDrawer
	{
		public static readonly Color ClipLabelColor = new Color(0f, 0.9f, 0.5f);
		public static readonly Color PlayButtonColor = new Color(0.25f, 0.9f, 0.25f, 0.4f);
		public static readonly Color StopButtonColor = new Color(0.9f, 0.25f, 0.25f, 0.4f);
		public static readonly Color WaveformMaskColor = new Color(0.05f, 0.05f, 0.05f, 0.3f);
		protected const float ClipViewHeight = 100f;
		protected const int ClipPropertiesLineCount = 4;

		public GUIStyleHelper GUIStyle = GUIStyleHelper.Instance;

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private float[] FadeValues = new float[2];
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private float[] PlaybackValues = new float[2];
		private Dictionary<string, ReorderableList> _reorderableListDict = new Dictionary<string, ReorderableList>();

		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get; set; }
		public bool IsEnable { get; private set; } = false;

		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawAdditionalClipProperties(Rect position, SerializedProperty property);

		public Rect GetRectAndIterateLine(Rect position)
		{
			return EditorScriptingExtension.GetRectAndIterateLine(this, position);
		}

		private void Enable()
		{
			BroAudioEditorWindow.OnCloseEditorWindow += Disable;
			BroAudioEditorWindow.OnSelectLibrary += Disable;
			IsEnable = true;
		}

		private void Disable()
		{
			_reorderableListDict.Clear();
			BroAudioEditorWindow.OnCloseEditorWindow -= Disable;
			BroAudioEditorWindow.OnSelectLibrary -= Disable;
			IsEnable = false;
		}

		#region Unity Entry Overrider
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(!IsEnable)
			{
				Enable();
			}

			EditorGUIUtility.wideMode = true;
			DrawLineCount = 0;
			SerializedProperty nameProp = property.FindPropertyRelative("Name");

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (property.isExpanded)
			{
				nameProp.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProp.stringValue);
				DrawAdditionalBaseProperties(position, property);

				#region Clip Properties
				DrawReorderableClipsList(position, property, out var currSelectedClip);
				if (currSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip),out AudioClip audioClip))
				{
					DrawClipProperties(position, currSelectedClip, audioClip);
					DrawAdditionalClipProperties(position, property);

					SerializedProperty isShowClipProp = property.FindPropertyRelative("IsShowClipPreview");
					isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
					bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
					if (isShowPreview)
					{
						DrawClipPreview(position, currSelectedClip, audioClip);
					}
				}
				#endregion	
			}
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = SingleLineSpace;

			if (property.isExpanded)
			{
				if (_reorderableListDict.TryGetValue(property.propertyPath, out ReorderableList list))
				{
					height += list.GetHeight();

					bool isShowClipProp = 
						property.TryGetArrayPropertyElementAtIndex("Clips", list.index, out var clipProp) &&
						clipProp.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip);
					bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative("IsShowClipPreview").boolValue;

					if(!isShowClipProp)
					{
						height -= ClipPropertiesLineCount * SingleLineSpace;
					}
					if(isShowClipPreview)
					{
						height += ClipViewHeight;
					}
				}
				height += property.CountInProperty() * SingleLineSpace;
			}
			return height;
		} 
		#endregion

		private void DrawReorderableClipsList(Rect position, SerializedProperty property, out SerializedProperty outSelectedClip)
		{
			ReorderableList reorderableList = GetReorderableList(property);

			bool isMulticlips = reorderableList.count > 1;
			SetCurrentPlayMode(property, isMulticlips, out SerializedProperty playModeProp, out MulticlipsPlayMode currentPlayMode);
			int selectedIndex = reorderableList.index > 0 ? reorderableList.index : 0;
			SerializedProperty currSelectedClip = reorderableList.count > 0 ? reorderableList.serializedProperty.GetArrayElementAtIndex(selectedIndex) : null;
			outSelectedClip = currSelectedClip;

			reorderableList.draggable = true;
			reorderableList.drawHeaderCallback = OnDrawHeader;
			reorderableList.drawElementCallback = OnDrawElement;
			reorderableList.drawFooterCallback = OnDrawFooter;
			reorderableList.onAddCallback = OnAdd;
			reorderableList.onRemoveCallback = OnRemove;
			reorderableList.DoList(GetRectAndIterateLine(position));

			ReorderableList GetReorderableList(SerializedProperty property)
			{
				SerializedProperty clipsProp = property.FindPropertyRelative("Clips");
				if (!_reorderableListDict.ContainsKey(property.propertyPath))
				{
					_reorderableListDict.Add(property.propertyPath, new ReorderableList(clipsProp.serializedObject, clipsProp));
				}

				return _reorderableListDict[property.propertyPath];
			}

			void SetCurrentPlayMode(SerializedProperty property, bool isMulticlips, out SerializedProperty playModeProp, out MulticlipsPlayMode currentPlayMode)
			{
				playModeProp = property.FindPropertyRelative("MulticlipsPlayMode");
				if (!isMulticlips)
				{
					playModeProp.enumValueIndex = 0;
				}
				else if (isMulticlips && playModeProp.enumValueIndex == 0)
				{
					playModeProp.enumValueIndex = 1;
				}
				currentPlayMode = (MulticlipsPlayMode)playModeProp.enumValueIndex;
			}

			#region ReorderableList Callback
			void OnDrawHeader(Rect rect)
			{
				float[] ratio = { 0.2f, 0.5f, 0.18f, 0.12f };
				if (EditorScriptingExtension.TrySplitRectHorizontal(rect, ratio, 15f, out Rect[] newRects))
				{
					EditorGUI.LabelField(newRects[0], "Clips");
					if (isMulticlips)
					{
						playModeProp.enumValueIndex = (int)(MulticlipsPlayMode)EditorGUI.EnumPopup(newRects[1], currentPlayMode);
						switch (currentPlayMode)
						{
							case MulticlipsPlayMode.Sequence:
								EditorGUI.LabelField(newRects[ratio.Length - 1], "Index");
								break;
							case MulticlipsPlayMode.Random:
								EditorGUI.LabelField(newRects[ratio.Length - 1], "Weight");
								break;
						}
						EditorGUI.LabelField(newRects[1].DissolveHorizontal(0.4f), "(PlayMode)".SetColor(Color.gray), GUIStyle.RichText);
					}
					DrawLineCount++;
				}
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				SerializedProperty clipProp = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				SerializedProperty audioClipProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip));
				EditorScriptingExtension.SplitRectHorizontal(rect, 0.9f, 15f, out Rect clipRect, out Rect valueRect);
				EditorGUI.PropertyField(clipRect, audioClipProp, new GUIContent(""));

				switch (currentPlayMode)
				{
					case MulticlipsPlayMode.Single:
						break;
					case MulticlipsPlayMode.Sequence:
						EditorGUI.LabelField(valueRect, index.ToString(), GUIStyle.MiddleCenterText);
						break;
					case MulticlipsPlayMode.Random:
						SerializedProperty weightProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Weight));
						GUIStyle intFieldStyle = new GUIStyle(EditorStyles.numberField);
						intFieldStyle.alignment = TextAnchor.MiddleCenter;
						weightProp.intValue = EditorGUI.IntField(valueRect, weightProp.intValue, intFieldStyle);
						break;
				}
				DrawLineCount++;
			}

			void OnDrawFooter(Rect rect)
			{
				ReorderableList.defaultBehaviours.DrawFooter(rect, reorderableList);
				if (currSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
				{
					Rect labelRect = new Rect(rect);
					labelRect.y += 5f;
					EditorGUI.LabelField(labelRect, audioClip.name.SetColor(ClipLabelColor).ToBold(), GUIStyle.RichText);
				}
				DrawLineCount++;
			}

			void OnRemove(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
			}

			void OnAdd(ReorderableList list)
			{
				//list.serializedProperty.InsertArrayElementAtIndex(list.count - 1);
				//this is the same as below... it didn't insert "empty" element as described.
				ReorderableList.defaultBehaviours.DoAddButton(list);

				var clipProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
				BroAudioClip.ResetAllSerializedProperties(clipProp);
			}
			#endregion
		}

		private void DrawClipProperties(Rect position, SerializedProperty clipProp,AudioClip audioClip)
		{
			SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			SerializedProperty endPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
			SerializedProperty fadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
			SerializedProperty fadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));

			PlaybackValues[0] = startPosProp.floatValue;
			PlaybackValues[1] = endPosProp.floatValue;
			FadeValues[0] = fadeInProp.floatValue;
			FadeValues[1] = fadeOutProp.floatValue;

			DrawVolumeField(position, volumeProp);
			DrawPlaybackPositionField(position, startPosProp, endPosProp, fadeInProp, fadeOutProp);
			DrawFadingField(position, startPosProp, endPosProp, fadeInProp, fadeOutProp);

			void DrawVolumeField(Rect position, SerializedProperty volumeProp)
			{
				volumeProp.floatValue = EditorGUI.Slider(GetRectAndIterateLine(position), nameof(BroAudioClip.Volume), volumeProp.floatValue, 0f, 1f);
			}

			void DrawPlaybackPositionField(Rect position, SerializedProperty startPosProp, SerializedProperty endPosProp, SerializedProperty fadeInProp, SerializedProperty fadeOutProp)
			{
				EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Playback Position"), PlaybackLabels, PlaybackValues);
				startPosProp.floatValue = Mathf.Clamp(PlaybackValues[0], 0f, GetLengthLimit(0, endPosProp.floatValue, fadeInProp.floatValue, fadeOutProp.floatValue, audioClip.length));
				PlaybackValues[0] = startPosProp.floatValue;
				endPosProp.floatValue = Mathf.Clamp(PlaybackValues[1], 0f, GetLengthLimit(startPosProp.floatValue, 0f, fadeInProp.floatValue, fadeOutProp.floatValue, audioClip.length));
				PlaybackValues[1] = endPosProp.floatValue;
			}

			void DrawFadingField(Rect position, SerializedProperty startPosProp, SerializedProperty endPosProp, SerializedProperty fadeInProp, SerializedProperty fadeOutProp)
			{
				EditorGUI.MultiFloatField(GetRectAndIterateLine(position), new GUIContent("Fade"), FadeLabels, FadeValues);
				fadeInProp.floatValue = Mathf.Clamp(FadeValues[0], 0f, GetLengthLimit(startPosProp.floatValue, endPosProp.floatValue, 0f, fadeOutProp.floatValue, audioClip.length));
				FadeValues[0] = fadeInProp.floatValue;
				fadeOutProp.floatValue = Mathf.Clamp(FadeValues[1], 0f, GetLengthLimit(startPosProp.floatValue, endPosProp.floatValue, fadeInProp.floatValue, 0f, audioClip.length));
				FadeValues[1] = fadeOutProp.floatValue;
			}
		}

		private void DrawClipPreview(Rect position, SerializedProperty clipProp, AudioClip audioClip)
		{
			Rect clipViewRect = GetRectAndIterateLine(position);
			clipViewRect.height = ClipViewHeight;
			EditorScriptingExtension.SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);

			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			DrawWaveformPreview(waveformRect);
			DrawPlaybackButton(startPosProp.floatValue, playbackRect);
			DrawClipPlaybackLine(waveformRect);

			void DrawWaveformPreview(Rect waveformRect)
			{
				Texture2D waveformTexture = AssetPreview.GetAssetPreview(audioClip);
				if (waveformTexture != null)
				{
					GUI.DrawTexture(waveformRect, waveformTexture);
				}
				EditorGUI.DrawRect(waveformRect, WaveformMaskColor);
			}

			void DrawPlaybackButton(float startPos, Rect clipViewRect)
			{
				EditorScriptingExtension.SplitRectVertical(clipViewRect, 0.5f, 15f, out Rect playRect, out Rect stopRect);
				// 保持在正方形
				float maxHeight = playRect.height;
				playRect.width = Mathf.Clamp(playRect.width, playRect.width, maxHeight);
				playRect.height = playRect.width;
				stopRect.width = playRect.width;
				stopRect.height = playRect.height;

				if (GUI.Button(playRect, EditorGUIUtility.IconContent("d_PlayButton")))
				{
					EditorPlayAudioClip.StopAllClips();
					EditorPlayAudioClip.PlayClip(audioClip, Mathf.RoundToInt(AudioSettings.outputSampleRate * startPos));
				}
				if (GUI.Button(stopRect, EditorGUIUtility.IconContent("d_PreMatQuad")))
				{
					EditorPlayAudioClip.StopAllClips();
				}

				EditorGUI.DrawRect(playRect, PlayButtonColor);
				EditorGUI.DrawRect(stopRect, StopButtonColor);
			}

			void DrawClipPlaybackLine(Rect waveformRect)
			{
				Vector3[] points = GetClipLinePoints(waveformRect.width, audioClip.length);
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
		}

		private Vector3[] GetClipLinePoints(float width,float clipLength)
		{
			if (width <= 0f)
				return new Vector3[0];

			Vector3[] points = new Vector3[4];
			// Start
			points[0] = new Vector3(Mathf.Lerp(0f, width, PlaybackValues[0] / clipLength), ClipViewHeight, 0f);
			// FadeIn
			points[1] = new Vector3(Mathf.Lerp(0f, width, (PlaybackValues[0] + FadeValues[0]) / clipLength), 0f, 0f);
			// FadeOut
			points[2] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1] - FadeValues[1]) / clipLength), 0f, 0f);
			// End
			points[3] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1]) / clipLength), ClipViewHeight, 0f);

			return points;
		}

		private float GetLengthLimit(float start, float end, float fadeIn, float fadeOut,float clipLength)
		{
			return clipLength - start - end - fadeIn - fadeOut;
		}
	}
}