using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using static MiProduction.Extension.EditorScriptingExtension;
using static MiProduction.BroAudio.AssetEditor.BroAudioGUISetting;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.AssetEditor
{
	public abstract class AudioLibraryPropertyDrawer : PropertyDrawer, IEditorDrawLineCounter
	{
		protected const float ClipPreviewHeight = 100f;

		public GUIStyleHelper GUIStyle = GUIStyleHelper.Instance;

		private GUIContent[] FadeLabels = { new GUIContent("    In    "), new GUIContent(" Out ") };
		private float[] FadeValues = new float[2];
		private GUIContent[] PlaybackLabels = { new GUIContent(" Start "), new GUIContent(" End ") };
		private float[] PlaybackValues = new float[2];

		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();

		private BroAudioEditorWindow _editorWindow = null;

		public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public int DrawLineCount { get; set; }
		public bool IsEnable { get; private set; } = false;
		protected abstract int BasePropertiesLineCount { get; }
		protected abstract int ClipPropertiesLineCount { get; }

		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawAdditionalClipProperties(Rect position, SerializedProperty property);

		public Rect GetRectAndIterateLine(Rect position)
		{
			return EditorScriptingExtension.GetRectAndIterateLine(this, position);
		}

		private void Enable()
		{
			_editorWindow = EditorWindow.GetWindow(typeof(BroAudioEditorWindow)) as BroAudioEditorWindow;
			_editorWindow.OnCloseEditorWindow += Disable;
			_editorWindow.OnSelectAsset += Disable;
			IsEnable = true;
		}

		private void Disable()
		{
			_reorderableClipsDict.Clear();

			_editorWindow.OnCloseEditorWindow -= Disable;
			_editorWindow.OnSelectAsset -= Disable;
			_editorWindow = null;
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
			SerializedProperty nameProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IAudioEntity.Name)));

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (property.isExpanded)
			{
				nameProp.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProp.stringValue);
				DrawAdditionalBaseProperties(position, property);

				#region Clip Properties
				ReorderableClips currClipList = DrawReorderableClipsList(position, property);
				SerializedProperty currSelectClip = currClipList.CurrentSelectedClip;
				if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.OriginAudioClip),out AudioClip audioClip))
				{
					DrawClipProperties(position, currClipList, audioClip);
					DrawAdditionalClipProperties(position, property);

					SerializedProperty isShowClipProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IAudioLibraryEditorProperty.IsShowClipPreview)));
					isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
					bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
					if (isShowPreview)
					{
						DrawClipPreview(position, currSelectClip, audioClip);
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
				if (_reorderableClipsDict.TryGetValue(property.propertyPath, out ReorderableClips clipList))
				{
					height += clipList.Height;
					bool isShowClipProp =
						clipList.CurrentSelectedClip != null &&
						clipList.CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.OriginAudioClip), out AudioClip audioClip);
					bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative(GetBackingFieldName(nameof(IAudioLibraryEditorProperty.IsShowClipPreview))).boolValue;

					if(isShowClipProp)
					{
						height += ClipPropertiesLineCount * SingleLineSpace;
					}
					if(isShowClipPreview)
					{
						height += ClipPreviewHeight;
					}
				}
				height += BasePropertiesLineCount * SingleLineSpace;
			}
			return height;
		} 
		#endregion

		private ReorderableClips DrawReorderableClipsList(Rect position, SerializedProperty property)
		{
			bool hasReorderableClips = _reorderableClipsDict.TryGetValue(property.propertyPath, out var reorderableClips);
			if (!hasReorderableClips)
			{
				reorderableClips = new ReorderableClips(property,this);
				_reorderableClipsDict.Add(property.propertyPath, reorderableClips);
			}

			reorderableClips.DrawReorderableList(GetRectAndIterateLine(position));
			return reorderableClips;
		}

		private void DrawClipProperties(Rect position,ReorderableClips reorderableClips ,AudioClip audioClip)
		{
			SerializedProperty clipProp = reorderableClips.CurrentSelectedClip;
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

			EditorGUI.BeginChangeCheck();
			DrawPlaybackPositionField(position, startPosProp, endPosProp, fadeInProp, fadeOutProp);
			DrawFadingField(position, startPosProp, endPosProp, fadeInProp, fadeOutProp);
		    if(EditorGUI.EndChangeCheck())
			{
				_editorWindow.PendingUpdatesController.CheckChanges(reorderableClips.GetCurrentSelectedClipChanges());
			}

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
			clipViewRect.height = ClipPreviewHeight;
			SplitRectHorizontal(clipViewRect, 0.1f, 15f, out Rect playbackRect, out Rect waveformRect);

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
				SplitRectVertical(clipViewRect, 0.5f, 15f, out Rect playRect, out Rect stopRect);
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
				leftBlock[3] = new Vector3(0f, ClipPreviewHeight, 0f);
				Handles.DrawAAConvexPolygon(leftBlock);

				Vector3[] rightBlock = new Vector3[4];
				rightBlock[0] = points[2];
				rightBlock[1] = new Vector3(waveformRect.width, 0f, 0f);
				rightBlock[2] = new Vector3(waveformRect.width, ClipPreviewHeight, 0f);
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
			points[0] = new Vector3(Mathf.Lerp(0f, width, PlaybackValues[0] / clipLength), ClipPreviewHeight, 0f);
			// FadeIn
			points[1] = new Vector3(Mathf.Lerp(0f, width, (PlaybackValues[0] + FadeValues[0]) / clipLength), 0f, 0f);
			// FadeOut
			points[2] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1] - FadeValues[1]) / clipLength), 0f, 0f);
			// End
			points[3] = new Vector3(Mathf.Lerp(0f, width, (clipLength - PlaybackValues[1]) / clipLength), ClipPreviewHeight, 0f);

			return points;
		}

		private float GetLengthLimit(float start, float end, float fadeIn, float fadeOut,float clipLength)
		{
			return clipLength - start - end - fadeIn - fadeOut;
		}
	}
}