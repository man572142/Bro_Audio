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
using static Ami.Extension.EditorVersionAdapter;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioLibrary))]
	public partial class AudioLibraryPropertyDrawer : MiPropertyDrawer
	{
		protected const float ClipPreviewHeight = 100f;
		private const int _basePropertiesLineCount = 1;
		private const int _clipPropertiesLineCount = 1;
		private const float _lowVolumeSnappingThreshold = 0.05f;
		private const float _highVolumeSnappingThreshold = 0.2f;
		private const string _dbValueStringFormat = "0.##";

		private GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume));

		private bool _hasOpenedLibraryManager = false;
		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private LibraryManagerWindow _editorWindow = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		private Dictionary<string, ITransport> _clipTransport = new Dictionary<string, ITransport>();
		
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public EditorSetting EditorSetting => BroEditorUtility.EditorSetting;

		protected override void OnEnable()
		{
			base.OnEnable();
			_hasOpenedLibraryManager = HasOpenEditorWindow<LibraryManagerWindow>();

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
			_clipTransport.Clear();

			if (_editorWindow)
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
			
			SerializedProperty nameProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IAudioLibrary.Name)));

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (!property.isExpanded || !EditorSetting.TryGetAudioTypeSetting(GetAudioType(property), out var setting))
			{
				return;
			}

			nameProp.stringValue = EditorGUI.TextField(GetRectAndIterateLine(position), "Name", nameProp.stringValue);
			DrawAdditionalBaseProperties(position, property, setting);

			#region Clip Properties
			ReorderableClips currClipList = DrawReorderableClipsList(position, property, OnClipChanged);
			SerializedProperty currSelectClip = currClipList.CurrentSelectedClip;
			//Debug.Log(currSelectClip.propertyPath);
			if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
			{
				DrawClipProperties(position, currSelectClip, audioClip, setting, out ITransport transport);
				DrawAdditionalClipProperties(position, property, setting);
				if (setting.DrawedProperty.HasFlag(DrawedProperty.ClipPreview))
				{
					SerializedProperty isShowClipProp = property.FindPropertyRelative(AudioLibrary.NameOf.IsShowClipPreview);
					isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
					bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
					if (isShowPreview)
					{
						_clipPropHelper.DrawClipPreview(GetRectAndIterateLine(position), transport, audioClip, currSelectClip.propertyPath);
					}
				}
			}
			#endregion
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = SingleLineSpace;
			
			if (property.isExpanded)
			{
				BroAudioType audioType = GetAudioType(property);
				if(!EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
				{
					return height;
				}

                height += (_basePropertiesLineCount + GetAdditionalBaseProtiesLineCount(property, setting)) * SingleLineSpace;
                if (_reorderableClipsDict.TryGetValue(property.propertyPath, out ReorderableClips clipList))
				{
					height += clipList.Height;
					bool isShowClipProp =
						clipList.CurrentSelectedClip != null &&
						clipList.CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip _);
					bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative(AudioLibrary.NameOf.IsShowClipPreview).boolValue;

					if(isShowClipProp)
					{
						height += (_clipPropertiesLineCount + GetAdditionalClipPropertiesLineCount(property, setting)) * SingleLineSpace;
					}
					if(isShowClipPreview)
					{
						height += ClipPreviewHeight;
					}
				}
				
			}
			return height;
		}
		#endregion

		private ReorderableClips DrawReorderableClipsList(Rect position, SerializedProperty property,Action<string> onClipChanged)
		{
			bool hasReorderableClips = _reorderableClipsDict.TryGetValue(property.propertyPath, out var reorderableClips);
			if (!hasReorderableClips)
			{
				reorderableClips = new ReorderableClips(property,this);
				_reorderableClipsDict.Add(property.propertyPath, reorderableClips);
			}

			reorderableClips.DrawReorderableList(GetRectAndIterateLine(position));
			reorderableClips.OnAudioClipChanged = onClipChanged;
			return reorderableClips;
		}

		private void DrawClipProperties(Rect position,SerializedProperty clipProp, AudioClip audioClip, EditorSetting.AudioTypeSetting setting, out ITransport transport)
		{
			SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			SerializedProperty endPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
			SerializedProperty fadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
			SerializedProperty fadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));
			SerializedProperty snapVolProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.SnapToFullVolume));

			if(setting.DrawedProperty.HasFlag(DrawedProperty.Volume))
			{
                Rect volRect = GetRectAndIterateLine(position);
                volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue, snapVolProp.boolValue, () =>
                {
                    snapVolProp.boolValue = !snapVolProp.boolValue;
                });
            }

			if(!_clipTransport.TryGetValue(clipProp.propertyPath,out transport))
			{
				transport = new TransportSerializedWrapper(startPosProp, endPosProp, fadeInProp, fadeOutProp, audioClip.length);
				_clipTransport.Add(clipProp.propertyPath, transport);
			}

			if (setting.DrawedProperty.HasFlag(DrawedProperty.PlaybackPosition))
			{
                Rect playbackRect = GetRectAndIterateLine(position);
                playbackRect.width *= 0.8f;
                _clipPropHelper.DrawPlaybackPositionField(playbackRect, transport);
            }

            if (setting.DrawedProperty.HasFlag(DrawedProperty.Fade))
			{
                Rect fadingRect = GetRectAndIterateLine(position);
                fadingRect.width *= 0.8f;
                _clipPropHelper.DrawFadingField(fadingRect, transport);
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
				if (BroEditorUtility.EditorSetting.ShowVUColorOnVolumeSlider)
				{
					DrawVUMeter(sliderRect, BroAudioGUISetting.VUMaskColor);
				}

				if (isSnap && CanSnap(currentValue))
				{
					currentValue = FullVolume;
				}

				float sliderFullScale = FullVolume / (FullDecibelVolume - MinDecibelVolume / DecibelVoulumeFullScale);
				DrawFullVolumeSnapPoint(sliderRect, sliderFullScale, onSwitchBoostMode);

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

			void DrawDecibelValueLabel(Rect dbRect, float value)
			{
				value = Mathf.Log10(value) * 20f;
				string plusSymbol = value > 0 ? "+" : string.Empty;
				string volText = plusSymbol + value.ToString(_dbValueStringFormat) + "dB";
				EditorGUI.LabelField(dbRect, volText);
			}

#if !UNITY_WEBGL
			void DrawVUMeter(Rect vuRect, Color maskColor)
			{
				vuRect.height *= 0.5f;
				EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent(IconConstant.HorizontalVUMeter).image);
				EditorGUI.DrawRect(vuRect, maskColor);
			}

			void DrawFullVolumeSnapPoint(Rect sliderPosition,float sliderFullScale ,Action onSwitchSnapMode)
			{
				Rect rect = new Rect(sliderPosition);
				rect.width = 30f;
				rect.x = sliderPosition.x + sliderPosition.width * (FullVolume / sliderFullScale) - (rect.width * 0.5f) + 1f; // add 1 pixel for more precise position
				rect.y -= sliderPosition.height;
				var icon = EditorGUIUtility.IconContent(IconConstant.VolumeSnapPointer);
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

			float ConvertToSliderValue(float vol, float sliderFullScale)
			{
				if(vol > FullVolume)
				{
					float db = vol.ToDecibel(true);
					return (db - MinDecibelVolume) / DecibelVoulumeFullScale * sliderFullScale;
				}
				return vol;
				
			}

			float ConvertToNomalizedVolume(float sliderValue,float sliderFullScale)
			{
				if(sliderValue > FullVolume)
				{
					float db = MinDecibelVolume + (sliderValue / sliderFullScale) * DecibelVoulumeFullScale;
					return db.ToNormalizeVolume(true);
				}
				return sliderValue;
			}

			bool CanSnap(float value)
			{
				float difference = value - FullVolume;
				bool isInLowVolumeSnappingRange = difference < 0f && difference * -1f <= _lowVolumeSnappingThreshold;
				bool isInHighVolumeSnappingRange = difference > 0f && difference <= _highVolumeSnappingThreshold;
				return isInLowVolumeSnappingRange || isInHighVolumeSnappingRange;
			}
#endif
		}

		private BroAudioType GetAudioType(SerializedProperty property)
		{
			int id = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioLibrary.ID))).intValue;
			return Utility.GetAudioType(id);
		}

		private void OnClipChanged(string clipPropPath)
		{
			_clipTransport.Remove(clipPropPath);
		}
    }
}