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
		public class ClipData
		{
			public ITransport Transport;
			public bool IsSnapToFullVolume;
		}

		public enum Tab { Clips, Overall}

		public static event Action OnEntityNameChanged;

		private const float ClipPreviewHeight = 100f;
		private const float DefaultFieldRatio = 0.9f;
		private const float PreviewPrettinessOffsetY = 7f; // for prettiness
		private const float FoldoutArrowWidth = 15f;
		private const float MaxTextFieldWidth = 300f;

		private readonly GUIContent[] _tabLabelGUIContents = { new GUIContent(nameof(Tab.Clips)), new GUIContent(nameof(Tab.Overall)) };
		private readonly float[] _tabLabelRatios = new float[] { 0.5f, 0.5f };
		private readonly float[] _identityLabelRatios = new float[] { 0.65f, 0.15f, 0.2f };
		private readonly GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume),"The playback volume of this clip");
        private readonly BroInstructionHelper _instruction = new BroInstructionHelper();
        private readonly IUniqueIDGenerator _idGenerator = new IdGenerator();
        private Rect[] _tabPreAllocRects = null;
		private Rect[] _identityLabelRects = null;

		private Dictionary<string, ReorderableClips> _reorderableClipsDict = new Dictionary<string, ReorderableClips>();
		private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper(ClipPreviewHeight);
		private Dictionary<string, ClipData> _clipDataDict = new Dictionary<string, ClipData>();
		private Dictionary<string, Tab> _currSelectedTabDict = new Dictionary<string, Tab>();
        private GenericMenu _changeAudioTypeOption = null;
		private SerializedProperty _entityThatIsModifyingAudioType = null;
        
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

            _changeAudioTypeOption = CreateAudioTypeGenericMenu(Instruction.LibraryManager_ChangeEntityAudioType, OnChangeEntityAudioType);

        }

        private void OnDisable()
		{
			foreach(var reorderableClips in _reorderableClipsDict.Values)
			{
				reorderableClips.Dispose();
			}
			_reorderableClipsDict.Clear();
			_clipDataDict.Clear();

			LibraryManagerWindow.OnCloseLibraryManagerWindow -= OnDisable;
			LibraryManagerWindow.OnSelectAsset -= OnDisable;

			IsEnable = false;
		}

        private GenericMenu CreateAudioTypeGenericMenu(Instruction instruction, GenericMenu.MenuFunction2 onClickOption)
        {
            GenericMenu menu = new GenericMenu();
            GUIContent text = new GUIContent(_instruction.GetText(instruction));
            menu.AddItem(text, false, null);
            menu.AddSeparator(string.Empty);

            Utility.ForeachConcreteAudioType((audioType) =>
            {
                GUIContent optionName = new GUIContent(audioType.ToString());
                menu.AddItem(optionName, false, onClickOption, audioType);
            });

            return menu;
        }

        private void OnChangeEntityAudioType(object type)
        {
            if (type is BroAudioType audioType && _entityThatIsModifyingAudioType != null)
            {
                var idProp = _entityThatIsModifyingAudioType.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID)));
				if(Utility.GetAudioType(idProp.intValue) != audioType)
				{
                    idProp.intValue = _idGenerator.GetSimpleUniqueID(audioType);
                    idProp.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        #region Unity Entry Overrider
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(position, property, label);
			property.serializedObject.Update();

            SerializedProperty nameProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IEntityIdentity.Name)));
			SerializedProperty idProp = property.FindPropertyRelative(GetBackingFieldName(nameof(IEntityIdentity.ID)));

            Rect foldoutRect = GetRectAndIterateLine(position);
			_identityLabelRects = _identityLabelRects ?? new Rect[_identityLabelRatios.Length];

            SplitRectHorizontal(foldoutRect, 5f, _identityLabelRects, _identityLabelRatios);
			Rect nameRect = _identityLabelRects[0]; Rect idRect = _identityLabelRects[1]; Rect audioTypeRect = _identityLabelRects[2];

            property.isExpanded = EditorGUI.Foldout(nameRect, property.isExpanded, property.isExpanded? string.Empty : nameProp.stringValue);
			DrawIdText(idRect, idProp.intValue);
			DrawAudioTypeButton(audioTypeRect, property, Utility.GetAudioType(idProp.intValue));
			if (!property.isExpanded || !TryGetAudioTypeSetting(property, out var setting))
			{
				return;
			}

			DrawEntityNameField(nameRect, nameProp);

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
						if (setting.DrawedProperty.Contains(DrawedProperty.ClipPreview) && audioClip != null)
						{
							DrawEmptyLine(1);
							Rect previewRect = GetNextLineRect(position);  
							previewRect.y -= PreviewPrettinessOffsetY;
							_clipPropHelper.DrawClipPreview(previewRect, transport, audioClip, currSelectClip.propertyPath);
							Offset += ClipPreviewHeight + ClipPreviewPadding;
						}
					}
					break;
				case Tab.Overall:
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
					case Tab.Overall:
						height += GetAdditionalBaseProtiesLineCount(property, setting) * SingleLineSpace + GetAdditionalBasePropertiesOffest(setting);
						break; 
				}
				height += SingleLineSpace * 0.5f; // compensation for tab label's half line height
				return height;
			}
		}

		private void DrawIdText(Rect position, int id)
		{
#if BroAudio_DevOnly
			Rect labelRect = new Rect(position) { width = 100f, x = position.xMax -100f};
			EditorGUI.LabelField(labelRect, $"ID: {id}", EditorStyles.label); 
#endif
		}

        private void DrawAudioTypeButton(Rect position, SerializedProperty property, BroAudioType audioType)
        {
			if(GUI.Button(position, string.Empty))
			{
				_entityThatIsModifyingAudioType = property;
                _changeAudioTypeOption.DropDown(position);
            }
            string audioTypeName = audioType == BroAudioType.None ? "Undefined Type" : audioType.ToString();
            EditorGUI.DrawRect(position.PolarCoordinates(-1f), EditorSetting.GetAudioTypeColor(audioType));
			EditorGUI.LabelField(position, audioTypeName, GUIStyleHelper.MiddleCenterText);
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
#if !UNITY_2019_3_OR_NEWER
                height += SingleLineSpace;
#endif
                GetOrAddTabDict(property.propertyPath, out Tab tab);
				switch (tab)
				{
					case Tab.Clips:
						height += GetClipListHeight(property, setting);
						break;
					case Tab.Overall:
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

				height += clipList.Height;
				height += isShowClipProp ? GetAdditionalClipPropertiesLineCount(property, setting) * SingleLineSpace : 0f;
				height += isShowClipProp ? ClipPreviewHeight + ClipPreviewPadding : 0f;
			}
			return height;
		}
		#endregion

		private void DrawEntityNameField(Rect position, SerializedProperty nameProp)
        {
            EditorGUI.BeginChangeCheck();
#if UNITY_2019_3_OR_NEWER
			Rect nameRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
			nameRect.x += FoldoutArrowWidth;
			nameRect.width = Mathf.Min(nameRect.width - FoldoutArrowWidth, MaxTextFieldWidth);
			nameRect.y += 1f;
#else
			Rect nameRect = new Rect(GetRectAndIterateLine(position));
#endif
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
			delay = delayProp.floatValue;

			if (!_clipDataDict.TryGetValue(clipProp.propertyPath, out var clipData))
			{
				clipData = new ClipData();
				clipData.Transport = new SerializedTransport(startPosProp, endPosProp, fadeInProp, fadeOutProp, delayProp, audioClip.length); ;
				_clipDataDict[clipProp.propertyPath] = clipData;
			}
			transport = clipData.Transport;

			if (CanDraw(DrawedProperty.Volume))
			{
                Rect volRect = GetRectAndIterateLine(position);
				volRect.width *= DefaultFieldRatio;
                volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue, clipData.IsSnapToFullVolume, () =>
                {
					clipData.IsSnapToFullVolume = !clipData.IsSnapToFullVolume;
                });
            }

			if (CanDraw(DrawedProperty.PlaybackPosition))
			{
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
			int id = property.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID))).intValue;
			return EditorSetting.TryGetAudioTypeSetting(Utility.GetAudioType(id), out setting);
		}

		private void OnClipChanged(string clipPropPath)
		{
			_clipDataDict.Remove(clipPropPath);
		}
	}
}