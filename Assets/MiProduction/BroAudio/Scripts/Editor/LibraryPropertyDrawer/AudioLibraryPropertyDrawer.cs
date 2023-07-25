using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using MiProduction.BroAudio.Editor.Setting;
using System;
using static MiProduction.Extension.EditorScriptingExtension;
using static MiProduction.Extension.AudioConstant;


namespace MiProduction.BroAudio.Editor
{
	public abstract class AudioLibraryPropertyDrawer : MiPropertyDrawer
	{
		protected const float ClipPreviewHeight = 100f;
		private const int _basePropertiesLineCount = 2;
		private const int _clipPropertiesLineCount = 4;
		private const float _fullVolumeSnappingThreshold = 0.1f;

		private GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume));

		private bool _hasOpenedLibraryManager = false;
		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private LibraryManagerWindow _editorWindow = null;
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

		protected abstract int AdditionalBasePropertiesLineCount { get; }
		protected abstract int AdditionalClipPropertiesLineCount { get; }
		protected int BasePropertiesLineCount => _basePropertiesLineCount + AdditionalBasePropertiesLineCount;
		protected int ClipPropertiesLineCount => _clipPropertiesLineCount + AdditionalClipPropertiesLineCount;

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

		private float DrawVolumeSlider(Rect position, GUIContent label, float currentValue,bool isSnap,Action onSwitchBoostMode)
		{
			float sliderFullScale = FullVolume / (FullDecibelVolume - MinDecibelVolume / DecibelVoulumeFullScale);

			Rect suffixRect = EditorGUI.PrefixLabel(position, label);
			if (TrySplitRectHorizontal(suffixRect, new float[] { 0.7f, 0.1f, 0.2f }, 10f, out Rect[] rects))
			{
				Rect sliderRect = rects[0];
				Rect fieldRect = rects[1];
				Rect dbLabelRect = rects[2];

				DrawVUMeter(sliderRect, new Color(0.2f, 0.2f, 0.2f, 0.8f));

				if (isSnap && Mathf.Abs(currentValue - FullVolume) <= _fullVolumeSnappingThreshold)
				{
					currentValue = FullVolume;
				}
				float sliderValue = ConvertToSliderValue(currentValue);
				sliderValue = GUI.HorizontalSlider(sliderRect, sliderValue, 0f, sliderFullScale);
				
				currentValue = EditorGUI.FloatField(fieldRect, ConvertToNomalizedVolume(sliderValue));

				DrawDecibelValueLabel(dbLabelRect, currentValue);
				DrawFullVolumeSnapPoint(sliderRect, isSnap, onSwitchBoostMode);
			}
			return currentValue;

			void DrawDecibelValueLabel(Rect position, float value)
			{
				value = Mathf.Log10(value) * 20f;
				string plusSymbol = value > 0 ? "+" : string.Empty;
				string volText = plusSymbol + value.ToString("0.##") + "dB";
				EditorGUI.LabelField(position, volText);
			}

			void DrawVUMeter(Rect vuRect, Color maskColor)
			{
				vuRect.height *= 0.5f;
				EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent("d_VUMeterTextureHorizontal").image);
				EditorGUI.DrawRect(vuRect, maskColor);
			}

			void DrawFullVolumeSnapPoint(Rect sliderPosition,bool isSnap, Action onSwitchSnapMode)
			{
				Rect rect = new Rect(sliderPosition);
				rect.width = 30f;
				// add 1 pixel for precise location
				rect.x = sliderPosition.x + sliderPosition.width * (FullVolume / sliderFullScale) - (rect.width * 0.5f) + 1f;
				rect.y -= position.height;
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

			float ConvertToSliderValue(float vol)
			{
				if(vol > FullVolume)
				{
					float db = vol.ToDecibel(true);
					return (db - MinDecibelVolume) / DecibelVoulumeFullScale * sliderFullScale;
				}
				return vol;
				
			}

			float ConvertToNomalizedVolume(float sliderValue)
			{
				if(sliderValue > FullVolume)
				{
					float db = MinDecibelVolume + (sliderValue / sliderFullScale) * DecibelVoulumeFullScale;
					return db.ToNormalizeVolume(true);
				}
				return sliderValue;
			}
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
    }
}