using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using Ami.BroAudio.Data;
using System;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
	[CustomPropertyDrawer(typeof(AudioEntity))]
	public partial class AudioEntityPropertyDrawer : MiPropertyDrawer
	{
		public enum Tab { Clips, Settings}

		public static event Action OnEntityNameChanged;

		private const float ClipPreviewHeight = 100f;
		private const float DefaultFieldRatio = 0.9f;

		private readonly GUIContent[] _tabLabelGUIContents = { new GUIContent(nameof(Tab.Clips)), new GUIContent(nameof(Tab.Settings)) };
		private readonly float[] _tabLabelRatios = new float[] { 0.5f, 0.5f };
		private GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume),"The playback volume of this clip");
		private Rect[] _tabPreAllocRects = null;

		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		private Dictionary<string, ITransport> _clipTransport = new Dictionary<string, ITransport>();
		private Dictionary<string, Tab> _currSelectedTabDict = new Dictionary<string, Tab>();
		
		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
		public float ClipPreviewPadding => ReorderableList.Defaults.padding;
		public float SnapVolumePadding => ReorderableList.Defaults.padding;

		private float TabLabelHeight => SingleLineSpace * 1.5f;

		protected override void OnEnable()
		{
			base.OnEnable();

			LibraryManagerWindow.OnCloseLibraryManagerWindow += OnDisable;
			LibraryManagerWindow.OnSelectAsset += OnDisable;
		}

		private void OnDisable()
		{
			foreach(var reorderableClips in _reorderableClipsDict.Values)
			{
				reorderableClips.Dispose();
			}
			_reorderableClipsDict.Clear();
			_clipTransport.Clear();

			LibraryManagerWindow.OnCloseLibraryManagerWindow -= OnDisable;
			LibraryManagerWindow.OnSelectAsset -= OnDisable;

			IsEnable = false;
		}

		#region Unity Entry Overrider
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(position, property, label);
			property.serializedObject.Update();

			SerializedProperty nameProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IEntityIdentity.Name)));

			property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, nameProp.stringValue);
			if (!property.isExpanded || !TryGetAudioTypeSetting(property, out var setting))
			{
				return;
			}

			DrawEntityNameField(position, nameProp);

			GetOrAddTabDict(property.propertyPath, out Tab tab);
			Rect tabViewRect = GetRectAndIterateLine(position);
			tabViewRect.height = GetTabWindowHeight();
			_tabPreAllocRects = _tabPreAllocRects ?? new Rect[_tabLabelRatios.Length];
			tab = (Tab)DrawTabsView(tabViewRect, (int)tab, TabLabelHeight, _tabLabelGUIContents, _tabLabelRatios, _tabPreAllocRects);
			_currSelectedTabDict[property.propertyPath] = tab;
			DrawEmptyLine(1);

			position.x += IndentInPixel;
			position.width -= IndentInPixel * 2f;

			switch (tab)
			{
				case Tab.Clips:
					ReorderableClips currClipList = DrawReorderableClipsList(position, property, OnClipChanged);
					SerializedProperty currSelectClip = currClipList.CurrentSelectedClip;
					if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
					{
						DrawClipProperties(position, currSelectClip, audioClip, setting, out ITransport transport, out float delay);
						DrawAdditionalClipProperties(position, currSelectClip, setting);
						if (setting.DrawedProperty.Contains(DrawedProperty.ClipPreview))
						{
							SerializedProperty isShowClipProp = property.FindPropertyRelative(AudioEntity.EditorPropertyName.IsShowClipPreview);
							isShowClipProp.boolValue = EditorGUI.Foldout(GetRectAndIterateLine(position), isShowClipProp.boolValue, "Preview");
							bool isShowPreview = isShowClipProp.boolValue && audioClip != null;
							if (isShowPreview)
							{
								_clipPropHelper.DrawClipPreview(GetNextLineRect(position), transport, audioClip, currSelectClip.propertyPath);
								Offset += ClipPreviewHeight + ClipPreviewPadding;
							}
						}
					}
					break;
				case Tab.Settings:
					Offset += SnapVolumePadding;
					DrawAdditionalBaseProperties(position, property, setting);
					break;
			}

			float GetTabWindowHeight()
			{
				float height = TabLabelHeight;
				switch (tab)
				{
					case Tab.Clips:
						height += GetClipListHeight(property, setting);
						break;
					case Tab.Settings:
						height += GetAdditionalBaseProtiesLineCount(property, setting) * SingleLineSpace + GetAdditionalBasePropertiesOffest(setting);
						break; 
				}
				height += SingleLineSpace * 0.5f; // compensation for tab label's half line height
				return height;
			}
		}

		private void GetOrAddTabDict(string propertyPath, out Tab tab)
		{
			if(!_currSelectedTabDict.TryGetValue(propertyPath,out tab))
			{
				_currSelectedTabDict[propertyPath] = Tab.Clips;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = SingleLineSpace; // Header

			if (property.isExpanded && TryGetAudioTypeSetting(property, out var setting))
			{
				height += ReorderableList.Defaults.padding; // reorderableList element padding;
				height += TabLabelHeight + SingleLineSpace * 0.5f; // Tab View + compensation
				GetOrAddTabDict(property.propertyPath, out Tab tab);
				switch (tab)
				{
					case Tab.Clips:
						height += GetClipListHeight(property, setting);
						break;
					case Tab.Settings:
						height += GetAdditionalBaseProtiesLineCount(property, setting) * SingleLineSpace + GetAdditionalBasePropertiesOffest(setting);
						break;
				}
			}
			return height;
		}

		private float GetClipListHeight(SerializedProperty property, EditorSetting.AudioTypeSetting setting)
		{
			float height = 0f;
			if (_reorderableClipsDict.TryGetValue(property.propertyPath, out ReorderableClips clipList))
			{
				bool isShowClipProp =
					clipList.CurrentSelectedClip != null &&
					clipList.CurrentSelectedClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip _);
				bool isShowClipPreview = isShowClipProp && property.FindPropertyRelative(AudioEntity.EditorPropertyName.IsShowClipPreview).boolValue;

				height += clipList.Height;
				height += isShowClipProp ? GetAdditionalClipPropertiesLineCount(property, setting) * SingleLineSpace : 0f;
				height += isShowClipPreview ? ClipPreviewHeight + ClipPreviewPadding : 0f;
			}
			return height;
		}
		#endregion

		private void DrawEntityNameField(Rect position, SerializedProperty nameProp)
        {
            EditorGUI.BeginChangeCheck();
			Rect nameRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight};
			nameRect.x += EditorGUIUtility.labelWidth;
			nameRect.width = nameRect.width * DefaultFieldRatio - EditorGUIUtility.labelWidth;
			nameRect.y += 1f;
			nameProp.stringValue = EditorGUI.TextField(nameRect, nameProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                OnEntityNameChanged?.Invoke();
            }
        }

        private ReorderableClips DrawReorderableClipsList(Rect position, SerializedProperty property,Action<string> onClipChanged)
		{
			bool hasReorderableClips = _reorderableClipsDict.TryGetValue(property.propertyPath, out var reorderableClips);
			if (!hasReorderableClips)
			{
				reorderableClips = new ReorderableClips(property);
				_reorderableClipsDict.Add(property.propertyPath, reorderableClips);
			}

			Rect rect = GetNextLineRect(position);
            reorderableClips.DrawReorderableList(rect);
			Offset += reorderableClips.Height;
			reorderableClips.OnAudioClipChanged = onClipChanged;
            return reorderableClips;
		}

		private void DrawClipProperties(Rect position,SerializedProperty clipProp, AudioClip audioClip, EditorSetting.AudioTypeSetting setting, out ITransport transport,out float delay)
		{
			SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
			SerializedProperty delayProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Delay));
			SerializedProperty startPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.StartPosition));
			SerializedProperty endPosProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.EndPosition));
			SerializedProperty fadeInProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeIn));
			SerializedProperty fadeOutProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.FadeOut));
			SerializedProperty snapVolProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.SnapToFullVolume));
			delay = delayProp.floatValue;

			if (CanDraw(DrawedProperty.Volume))
			{
                Rect volRect = GetRectAndIterateLine(position);
				volRect.width *= DefaultFieldRatio;
                volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue, snapVolProp.boolValue, () =>
                {
                    snapVolProp.boolValue = !snapVolProp.boolValue;
                });
            }

			if(!_clipTransport.TryGetValue(clipProp.propertyPath,out transport))
			{
				transport = new SerializedTransport(startPosProp, endPosProp, fadeInProp, fadeOutProp, delayProp,audioClip.length);
				_clipTransport.Add(clipProp.propertyPath, transport);
			}

			if (CanDraw(DrawedProperty.PlaybackPosition))
			{
				SerializedTransport wrapper = transport as SerializedTransport;
                Rect playbackRect = GetRectAndIterateLine(position);
                playbackRect.width *= DefaultFieldRatio;
                _clipPropHelper.DrawPlaybackPositionField(playbackRect, transport);
			}

            if (CanDraw(DrawedProperty.Fade))
			{
                Rect fadingRect = GetRectAndIterateLine(position);
                fadingRect.width *= DefaultFieldRatio;
                _clipPropHelper.DrawFadingField(fadingRect, transport);
            }

			bool CanDraw(DrawedProperty drawedProperty)
			{
				return setting.DrawedProperty.Contains(drawedProperty);
			}
		}

		private bool TryGetAudioTypeSetting(SerializedProperty property, out EditorSetting.AudioTypeSetting setting)
		{
			setting = default;
			IAudioAsset asset = property.serializedObject.targetObject as AudioAsset;
			return asset != null && EditorSetting.TryGetAudioTypeSetting(asset.AudioType, out setting);
		}

		private void OnClipChanged(string clipPropPath)
		{
			_clipTransport.Remove(clipPropPath);
		}
	}
}