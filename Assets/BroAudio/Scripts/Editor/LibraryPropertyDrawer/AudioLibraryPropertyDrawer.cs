using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.Extension.AudioConstant;


namespace Ami.BroAudio.Editor
{
	public abstract class AudioLibraryPropertyDrawer : MiPropertyDrawer
	{
		protected const float ClipPreviewHeight = 100f;
		private const int _basePropertiesLineCount = 2;
		private const int _clipPropertiesLineCount = 4;
		private const float _lowVolumeSnappingThreshold = 0.05f;
		private const float _highVolumeSnappingThreshold = 0.2f;
		private const string _dbValueStringFormat = "0.##";

		private GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume));

		private bool _hasOpenedLibraryManager = false;
		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private LibraryManagerWindow _editorWindow = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

		protected abstract int GetAdditionalBaseProtiesLineCount(SerializedProperty property);
		protected abstract int GetAdditionalClipPropertiesLineCount(SerializedProperty property);
		protected abstract void DrawAdditionalBaseProperties(Rect position, SerializedProperty property);
		protected abstract void DrawAdditionalClipProperties(Rect position, SerializedProperty property);

		protected override void OnEnable()
		{
			base.OnEnable();
			_hasOpenedLibraryManager = EditorWindow.HasOpenInstances<LibraryManagerWindow>();

			if(_hasOpenedLibraryManager)
			{
				_editorWindow = EditorWindow.GetWindow(typeof(LibraryManagerWindow)) as LibraryManagerWindow;
				_editorWindow.OnCloseLibraryManagerWindow += OnDisable;
				_editorWindow.OnSelectAsset += OnDisable;
			}
		}

		private void OnDisable()
		{
			_reorderableClipsDict.Clear();

			if(_editorWindow)
			{
				_editorWindow.OnCloseLibraryManagerWindow -= OnDisable;
				_editorWindow.OnSelectAsset -= OnDisable;
				_editorWindow = null;
			}
			
			IsEnable = false;
		}


		#region Unity Entry Overrider
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(position,property,label);
			if(!_hasOpenedLibraryManager)
			{
				return;
			}
			
			SerializedProperty nameProp = property.FindPropertyRelative(GetAutoBackingFieldName(nameof(IAudioLibrary.Name)));

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (property.isExpanded)
			{
				nameProp.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProp.stringValue);
				DrawAdditionalBaseProperties(position, property);

				#region Clip Properties
				ReorderableClips currClipList = DrawReorderableClipsList(position, property);
				SerializedProperty currSelectClip = currClipList.CurrentSelectedClip;
				if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip),out AudioClip audioClip))
				{
					DrawClipProperties(position, currClipList, audioClip,out Transport transport);
					DrawAdditionalClipProperties(position, property);
					DrawClipPreview(position, property, transport, audioClip);
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
						clipList.CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip);
					bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative(AudioLibrary.NameOf_IsShowClipPreview).boolValue;

					if(isShowClipProp)
					{
						height += (_clipPropertiesLineCount + GetAdditionalClipPropertiesLineCount(property)) * SingleLineSpace;
					}
					if(isShowClipPreview)
					{
						height += ClipPreviewHeight;
					}
				}
				height += (_basePropertiesLineCount + GetAdditionalBaseProtiesLineCount(property)) * SingleLineSpace;
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

		private void DrawClipProperties(Rect position,ReorderableClips reorderableClips ,AudioClip audioClip,out Transport transport)
		{
			SerializedProperty clipProp = reorderableClips.CurrentSelectedClip;
			SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			SerializedProperty endPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
			SerializedProperty fadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
			SerializedProperty fadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));
			SerializedProperty snapVolProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.SnapToFullVolume));

			transport = new Transport();
			transport.StartPosition = startPosProp.floatValue;
			transport.EndPosition = endPosProp.floatValue;
			transport.FadeIn = fadeInProp.floatValue;
			transport.FadeOut = fadeOutProp.floatValue;
			transport.FullLength = audioClip.length;

			_clipPropHelper.SetCurrentTransport(transport);

			Rect volRect = GetRectAndIterateLine(position);
			volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue,snapVolProp.boolValue, () => 
			{
				snapVolProp.boolValue = !snapVolProp.boolValue;
			});

			Rect playbackRect = GetRectAndIterateLine(position);
			playbackRect.width *= 0.8f;
			_clipPropHelper.DrawPlaybackPositionField(playbackRect, transport, out var newPos);
			startPosProp.floatValue = newPos.StartPosition;
			endPosProp.floatValue = newPos.EndPosition;

			Rect fadingRect = GetRectAndIterateLine(position);
			fadingRect.width *= 0.8f;
			_clipPropHelper.DrawFadingField(fadingRect, transport,out var newFading);
			fadeInProp.floatValue = newFading.FadeIn;
			fadeOutProp.floatValue = newFading.FadeOut;
		}

		private void DrawClipPreview(Rect position, SerializedProperty property, Transport transport, AudioClip audioClip)
		{
			SerializedProperty isShowClipProp = property.FindPropertyRelative(AudioLibrary.NameOf_IsShowClipPreview);
			isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
			bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
			if (isShowPreview)
			{
				_clipPropHelper.DrawClipPreview(GetRectAndIterateLine(position), transport, audioClip);
			}
		}

		private float DrawVolumeSlider(Rect position, GUIContent label, float currentValue,bool isSnap,Action onSwitchBoostMode)
		{
			Rect suffixRect = EditorGUI.PrefixLabel(position, label);
			if (TrySplitRectHorizontal(suffixRect, new float[] { 0.7f, 0.1f, 0.2f }, 10f, out Rect[] rects))
			{
				Rect sliderRect = rects[0];
				Rect fieldRect = rects[1];
				Rect dbLabelRect = rects[2];

#if !UNITY_WEBGL
				if (BroEditorUtility.GlobalSetting.ShowVUColorOnVolumeSlider)
				{
					DrawVUMeter(sliderRect, BroAudioGUISetting.VUMaskColor);
				}

				if (isSnap && CanSnap(currentValue))
				{
					currentValue = FullVolume;
				}

				float sliderFullScale = FullVolume / (FullDecibelVolume - MinDecibelVolume / DecibelVoulumeFullScale);
				DrawFullVolumeSnapPoint(sliderRect, sliderFullScale, isSnap, onSwitchBoostMode);

				float sliderValue = ConvertToSliderValue(currentValue, sliderFullScale);
				float newSliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, 0f, sliderFullScale);
				bool hasSliderChanged = sliderValue != newSliderValue;

				float newFloatFieldValue = EditorGUI.FloatField(fieldRect, hasSliderChanged ? ConvertToNomalizedVolume(newSliderValue, sliderFullScale) : currentValue);
				currentValue = Mathf.Clamp(newFloatFieldValue, 0f, MaxVolume);
#else
				currentValue = GUI.HorizontalSlider(sliderRect, currentValue, 0f, FullVolume);
				currentValue = Mathf.Clamp(EditorGUI.FloatField(fieldRect, currentValue),0f,FullVolume);
#endif

				DrawDecibelValueLabel(dbLabelRect, currentValue);
			}
			return currentValue;

			static void DrawDecibelValueLabel(Rect position, float value)
			{
				value = Mathf.Log10(value) * 20f;
				string plusSymbol = value > 0 ? "+" : string.Empty;
				string volText = plusSymbol + value.ToString(_dbValueStringFormat) + "dB";
				EditorGUI.LabelField(position, volText);
			}

#if !UNITY_WEBGL
			static void DrawVUMeter(Rect vuRect, Color maskColor)
			{
				vuRect.height *= 0.5f;
				EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent("d_VUMeterTextureHorizontal").image);
				EditorGUI.DrawRect(vuRect, maskColor);
			}

			static void DrawFullVolumeSnapPoint(Rect sliderPosition,float sliderFullScale,bool isSnap ,Action onSwitchSnapMode)
			{
				Rect rect = new Rect(sliderPosition);
				rect.width = 30f;
				rect.x = sliderPosition.x + sliderPosition.width * (FullVolume / sliderFullScale) - (rect.width * 0.5f) + 1f; // add 1 pixel for more precise position
				rect.y -= sliderPosition.height;
				var icon = EditorGUIUtility.IconContent("SignalAsset Icon");
				EditorGUI.BeginDisabledGroup(!isSnap);
				{
					GUI.Label(rect, icon);
				}
				EditorGUI.EndDisabledGroup();
				if (GUI.Button(rect, "", EditorStyles.label))
				{
					onSwitchSnapMode?.Invoke();
				}
			}

			static float ConvertToSliderValue(float vol, float sliderFullScale)
			{
				if(vol > FullVolume)
				{
					float db = vol.ToDecibel(true);
					return (db - MinDecibelVolume) / DecibelVoulumeFullScale * sliderFullScale;
				}
				return vol;
				
			}

			static float ConvertToNomalizedVolume(float sliderValue,float sliderFullScale)
			{
				if(sliderValue > FullVolume)
				{
					float db = MinDecibelVolume + (sliderValue / sliderFullScale) * DecibelVoulumeFullScale;
					return db.ToNormalizeVolume(true);
				}
				return sliderValue;
			}

			static bool CanSnap(float value)
			{
				float difference = value - FullVolume;
				bool isInLowVolumeSnappingRange = difference < 0f && difference * -1f <= _lowVolumeSnappingThreshold;
				bool isInHighVolumeSnappingRange = difference > 0f && difference <= _highVolumeSnappingThreshold;
				return isInLowVolumeSnappingRange || isInHighVolumeSnappingRange;
			}
#endif
		}
    }
}