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
        private class ClipData
        {
            public ITransport Transport;
            public bool IsSnapToFullVolume;
        }

        private class EntityData
        {
            public Tab SelectedTab;
            public bool IsLoop;
            public bool IsPreviewing;
            public readonly Rect[] HiddenButtonRects = new Rect[4];

            public bool IsPlaying => IsPreviewing || (Clips != null && Clips.IsPlaying);

            public ReorderableClips Clips { get; private set; }

            public EntityData(ReorderableClips clips)
            {
                Clips = clips;
            }

            public void Dispose()
            {
                Clips?.Dispose();
                Clips = null;
            }

            public void UpdateHiddenButtonRect(TransportType transportType, Rect rect)
            {
                int typeIndex = (int)transportType;
                if(typeIndex < 4)
                {
                    HiddenButtonRects[typeIndex] = rect;
                }
            }
        }

        public enum Tab { Clips, Overall}

        public static event Action OnEntityNameChanged;
        public static event Action OnRemoveEntity;
        public static event Action<bool> OnExpandAll;

        private const float ClipPreviewHeight = 100f;
        private const float DefaultFieldRatio = 0.9f;
        private const float PreviewPrettinessOffsetY = 7f; // for prettiness
        private const float FoldoutArrowWidth = 15f;
        private const float MaxTextFieldWidth = 300f;

        private readonly float[] _headerRatios = new float[] { 0.55f, 0.2f, 0.25f };
        private readonly GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume),"The playback volume of this clip");
        private readonly BroInstructionHelper _instruction = new BroInstructionHelper();
        private readonly IUniqueIDGenerator _idGenerator = new IdGenerator();
        private TabViewData[] _tabViewDatas = new TabViewData[]
            {
                new TabViewData(0.475f, new GUIContent(nameof(Tab.Clips)), EditorPlayAudioClip.Instance.StopAllClips, null),
                new TabViewData(0.475f, new GUIContent(nameof(Tab.Overall)), EditorPlayAudioClip.Instance.StopAllClips, null),
                new TabViewData(0.05f, EditorGUIUtility.IconContent("pane options"), null, OnOpenOptionMenu),
            };
        private Rect[] _headerRects = null;

        private DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper();
        private Dictionary<string, ClipData> _clipDataDict = new Dictionary<string, ClipData>();
        private Dictionary<string, EntityData> _entityDataDict = new Dictionary<string, EntityData>();
        private GenericMenu _changeAudioTypeOption = null;
        private SerializedProperty _entityThatIsModifyingAudioType = null;
        private AudioEntity _currentPreviewingEntity = null;

        public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
        public EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
        public float ClipPreviewPadding => ReorderableList.Defaults.padding;
        public float SnapVolumePadding => ReorderableList.Defaults.padding;
        private float TabLabelHeight => SingleLineSpace * 1.3f;
        private float TabLabelCompensation => SingleLineSpace * 2 - TabLabelHeight;

        protected override void OnEnable()
        {
            base.OnEnable();

            LibraryManagerWindow.OnCloseLibraryManagerWindow += OnDisable;
            LibraryManagerWindow.OnSelectAsset += OnDisable;
            LibraryManagerWindow.OnLostFocusEvent += OnLostFocus;

            _changeAudioTypeOption = CreateAudioTypeGenericMenu(Instruction.LibraryManager_ChangeEntityAudioType, OnChangeEntityAudioType);
        }

        private void OnLostFocus()
        {
            ResetPreview();
        }

        private void OnDisable()
        {
            foreach(var data in _entityDataDict.Values)
            {
                data.Dispose();
            }
            _entityDataDict.Clear();
            _clipDataDict.Clear();

            LibraryManagerWindow.OnCloseLibraryManagerWindow -= OnDisable;
            LibraryManagerWindow.OnSelectAsset -= OnDisable;
            LibraryManagerWindow.OnLostFocusEvent -= OnLostFocus;

            ResetPreview();

            OnEntityNameChanged = null;
            OnRemoveEntity = null;
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
                var idProp = _entityThatIsModifyingAudioType.FindBackingFieldProperty(nameof(AudioEntity.ID));
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

            SerializedProperty nameProp = property.FindBackingFieldProperty(nameof(IEntityIdentity.Name));
            SerializedProperty idProp = property.FindBackingFieldProperty(nameof(IEntityIdentity.ID));

            Rect foldoutRect = GetRectAndIterateLine(position);
            _headerRects ??= new Rect[_headerRatios.Length];

            float gap = 50f;
            SplitRectHorizontal(foldoutRect, gap, _headerRects, _headerRatios);
            Rect nameRect = _headerRects[0]; Rect previewButtonRect = _headerRects[1]; Rect audioTypeRect = _headerRects[2];
            audioTypeRect.x += gap * 0.5f;

            EditorGUI.BeginChangeCheck();
            property.isExpanded = EditorGUI.Foldout(nameRect, property.isExpanded, property.isExpanded? string.Empty : nameProp.stringValue);
            if(EditorGUI.EndChangeCheck() && Event.current.alt)
            {
                OnExpandAll?.Invoke(property.isExpanded);
            }

            DrawAudioTypeButton(audioTypeRect, property, Utility.GetAudioType(idProp.intValue));
            if (!property.isExpanded || !TryGetAudioTypeSetting(property, out var setting))
            {
                return;
            }

            GetOrCreateEntityDataDict(property, out var data);
            DrawEntityNameField(nameRect, nameProp, idProp.intValue);
            DrawEntityPreviewButton(previewButtonRect, property, data);

            _clipPropHelper.DrawDraggableHiddenButton(data.HiddenButtonRects, setting);

            Rect tabViewRect = GetRectAndIterateLine(position).SetHeight(GetTabWindowHeight());
            data.SelectedTab = (Tab)DrawButtonTabsMixedView(tabViewRect, property,(int)data.SelectedTab, TabLabelHeight, _tabViewDatas);
            
            DrawEmptyLine(1);

            position.x += IndentInPixel;
            position.width -= IndentInPixel * 2f;

            switch (data.SelectedTab)
            {
                case Tab.Clips:
                    DrawReorderableClipsList(position, data.Clips, OnClipChanged);
                    SerializedProperty currSelectClip = data.Clips.CurrentSelectedClip;
                    if (currSelectClip.TryGetPropertyObject(nameof(BroAudioClip.AudioClip), out AudioClip audioClip))
                    {
                        DrawClipProperties(position, currSelectClip, audioClip, setting, out ITransport transport, out float volume);
                        DrawAdditionalClipProperties(position, currSelectClip, setting);
                        if (setting.CanDraw(DrawedProperty.ClipPreview) && audioClip != null && Event.current.type != EventType.Layout)
                        {
                            DrawEmptyLine(1);
                            Rect previewRect = GetNextLineRect(position);  
                            previewRect.y -= PreviewPrettinessOffsetY;
                            previewRect.height = ClipPreviewHeight;
                            _clipPropHelper.DrawClipPreview(previewRect, transport, audioClip, volume, currSelectClip.propertyPath, data.Clips.SetPlayingClip, DrawPlaybackValuePeeking);
                            data.Clips.SetPreviewRect(previewRect);
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
                height += GetTabViewHeight(property, setting, data.SelectedTab);
                height += TabLabelCompensation;
                return height;
            }

            void DrawPlaybackValuePeeking(ITransport transport, TransportType transportType, Rect dragPointRect)
            {
                if (!setting.CanDraw(transportType.GetDrawedProperty()))
                {
                    data.UpdateHiddenButtonRect(transportType, dragPointRect);
                    if(dragPointRect.Contains(Event.current.mousePosition))
                    {
                        Rect rect = new Rect(dragPointRect) { width = 50f };
                        rect.y -= dragPointRect.height;
                        rect.x -= dragPointRect.width * 0.5f;
                        DrawValuePeeking(rect, transport.GetValue(transportType).ToString("0.000"));
                    }
                }
            }
        }

        private void GetOrCreateEntityDataDict(SerializedProperty property, out EntityData data)
        {
            if(!_entityDataDict.TryGetValue(property.propertyPath, out data))
            {
                var reorderableClips = new ReorderableClips(property);
                data = new EntityData(reorderableClips);
                _entityDataDict[property.propertyPath] = data;
            }
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = SingleLineSpace; // Header

            if (property.isExpanded && TryGetAudioTypeSetting(property, out var setting))
            {
                height += ReorderableList.Defaults.padding; // reorderableList element padding;
                height += TabLabelHeight + TabLabelCompensation;

                GetOrCreateEntityDataDict(property, out var data);

                height += GetTabViewHeight(property, setting, data.SelectedTab);
            }
            return height;
        }

        private float GetClipListHeight(SerializedProperty property, EditorSetting.AudioTypeSetting setting)
        {
            float height = 0f;
            if (_entityDataDict.TryGetValue(property.propertyPath, out var data))
            {
                bool isShowClipProp = data.Clips.HasValidClipSelected;

                height += data.Clips.Height;
                height += isShowClipProp ? GetAdditionalClipPropertiesHeight(property, setting) : 0f;
                height += isShowClipProp && setting.CanDraw(DrawedProperty.ClipPreview) ? ClipPreviewHeight + ClipPreviewPadding : 0f;
            }
            return height;
        }

        private float GetTabViewHeight(SerializedProperty property, EditorSetting.AudioTypeSetting setting, Tab tab) => tab switch
        {
            Tab.Clips => GetClipListHeight(property, setting),
            Tab.Overall => GetAdditionalBasePropertiesHeight(property, setting),
            _ => 0f,
        };
        #endregion

        private void DrawEntityNameField(Rect position, SerializedProperty nameProp, int id)
        {
            EditorGUI.BeginChangeCheck();
            Rect nameRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
            nameRect.x += FoldoutArrowWidth;
            nameRect.width = Mathf.Min(nameRect.width - FoldoutArrowWidth, MaxTextFieldWidth);
            nameRect.y += 1f;

            GUIContent content = new GUIContent();
#if !BroAudio_DevOnly
            content.tooltip = id.ToString(); 
#endif
            nameProp.stringValue = EditorGUI.TextField(nameRect, content, nameProp.stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                OnEntityNameChanged?.Invoke();
            }
        }

        private void DrawEntityPreviewButton(Rect rect, SerializedProperty property, EntityData data)
        {
            if(!data.Clips.HasValidClipSelected)
            {
                return;
            }

            rect = rect.SetHeight(h => h * 1.1f);
            SplitRectHorizontal(rect, 0.5f, 5f, out Rect playButtonRect, out Rect loopToggleRect);
            data.IsLoop = DrawButtonToggle(loopToggleRect, data.IsLoop, EditorGUIUtility.IconContent(IconConstant.LoopIcon));
            if(GUI.Button(playButtonRect, GetPlaybackButtonIcon(data.IsPlaying)) && TryGetEntityInstance(property, out var entity))
            {
                if (data.IsPlaying)
                {
                    EditorPlayAudioClip.Instance.StopAllClips();
                    Utility.ClearClipsSequencer();
                    entity.Clips.ResetIsUse();
                }
                else
                {
                    StartPreview(data, entity);
                }
            }

            if(data.IsPlaying && data.SelectedTab != Tab.Clips)
            {
                EditorPlayAudioClip.Instance.PlaybackIndicator.End();
            }
        }

        private void DrawAudioTypeButton(Rect position, SerializedProperty property, BroAudioType audioType)
        {
            if (GUI.Button(position, string.Empty))
            {
                _entityThatIsModifyingAudioType = property;
                _changeAudioTypeOption.DropDown(position);
            }
            string audioTypeName = audioType == BroAudioType.None ? "Undefined Type" : audioType.ToString();
            EditorGUI.DrawRect(position.PolarCoordinates(-1f), EditorSetting.GetAudioTypeColor(audioType));
            EditorGUI.LabelField(position, audioTypeName, GUIStyleHelper.MiddleCenterText);
        }

        private ReorderableClips DrawReorderableClipsList(Rect position, ReorderableClips reorderableClips, Action<string> onClipChanged)
        {
            Rect rect = GetNextLineRect(position);
            reorderableClips.DrawReorderableList(rect);
            Offset += reorderableClips.Height;
            reorderableClips.OnAudioClipChanged = onClipChanged;
            return reorderableClips;
        }

        private void DrawClipProperties(Rect position,SerializedProperty clipProp, AudioClip audioClip, EditorSetting.AudioTypeSetting setting, out ITransport transport,out float volume)
        {
            SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));

            if (!_clipDataDict.TryGetValue(clipProp.propertyPath, out var clipData))
            {
                clipData = new ClipData();
                clipData.Transport = new SerializedTransport(clipProp, audioClip.length); ;
                _clipDataDict[clipProp.propertyPath] = clipData;
            }
            transport = clipData.Transport;

            if (setting.CanDraw(DrawedProperty.Volume))
            {
                Rect volRect = GetRectAndIterateLine(position).SetWidth(w => w * DefaultFieldRatio);
                volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue, clipData.IsSnapToFullVolume, () =>
                {
                    clipData.IsSnapToFullVolume = !clipData.IsSnapToFullVolume;
                });
            }
            volume = volumeProp.floatValue;

            if (setting.CanDraw(DrawedProperty.PlaybackPosition))
            {
                Rect playbackRect = GetRectAndIterateLine(position).SetWidth(w => w * DefaultFieldRatio);
                _clipPropHelper.DrawPlaybackPositionField(playbackRect, transport);
            }

            if (setting.CanDraw(DrawedProperty.Fade))
            {
                Rect fadingRect = GetRectAndIterateLine(position).SetWidth(w => w * DefaultFieldRatio);
                _clipPropHelper.DrawFadingField(fadingRect, transport);
            }
        }

        private bool TryGetAudioTypeSetting(SerializedProperty property, out EditorSetting.AudioTypeSetting setting)
        {
            int id = property.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
            return EditorSetting.TryGetAudioTypeSetting(Utility.GetAudioType(id), out setting);
        }

        private void OnClipChanged(string clipPropPath)
        {
            _clipDataDict.Remove(clipPropPath);
        }

        private void StartPreview(EntityData data, AudioEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            _currentPreviewingEntity = entity;
            var clip = entity.PickNewClip(out int index);
            data.Clips.SelectAndSetPlayingElement(index);

            float volume = clip.Volume * entity.GetMasterVolume();
            float pitch = entity.GetPitch();
            Action onReplay = null;
            if (data.IsLoop)
            {
                onReplay = ReplayPreview;
            }
            var clipData = new EditorPlayAudioClip.Data(clip) { Volume = volume };
            EditorPlayAudioClip.Instance.PlayClipByAudioSource(clipData, false, onReplay, pitch);
            EditorPlayAudioClip.Instance.PlaybackIndicator.SetClipInfo(data.Clips.PreviewRect, new PreviewClip(clip), entity.GetPitch());
            data.IsPreviewing = true;
            EditorPlayAudioClip.Instance.OnFinished = OnPreviewFinished;

            void ReplayPreview()
            {
                StartPreview(data, entity);
            }

            void OnPreviewFinished()
            {
                data.IsPreviewing = false;
                data.Clips.SetPlayingClip(null);
            }
        }

        private void ResetPreview()
        {
            Utility.ClearClipsSequencer();
            _currentPreviewingEntity?.Clips?.ResetIsUse();
        }

        private bool TryGetEntityInstance(SerializedProperty property,out AudioEntity entity)
        {
            entity = null;
            object obj = fieldInfo.GetValue(property.serializedObject.targetObject);
            if (obj is AudioEntity[] entities)
            {
                string baseName = nameof(AudioAsset.Entities) + ".Array.data[";
                string num = property.propertyPath.Remove(property.propertyPath.Length - 1).Remove(0, baseName.Length);
                if (int.TryParse(num, out int index) && index < entities.Length)
                {
                    entity = entities[index];
                }
            }
            return entity != null;
        }

        private static void OnOpenOptionMenu(Rect rect, SerializedProperty property)
        {
            var idProp = property.FindBackingFieldProperty(nameof(AudioEntity.ID));
            var nameProp = property.FindBackingFieldProperty(nameof(AudioEntity.Name));

            GenericMenu menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent($"ID:{idProp.intValue}"));
#if BroAudio_DevOnly
            menu.AddItem(new GUIContent("Copy ID to the clipboard"), false, CopyID);
#endif
            menu.AddItem(new GUIContent($"Remove [{nameProp.stringValue}]"), false, () => OnRemoveEntity?.Invoke());

            var audioType = Utility.GetAudioType(idProp.intValue);
            if (!BroEditorUtility.EditorSetting.TryGetAudioTypeSetting(audioType, out var typeSetting))
            {
                return;
            }

            menu.AddSeparator(string.Empty);
            menu.AddDisabledItem(new GUIContent($"Displayed properties of AudioType.{audioType}"));
            ForeachConcreteDrawedProperty(OnAddMenuItem);
            menu.DropDown(rect);

            void OnAddMenuItem(DrawedProperty target)
            {
                menu.AddItem(new GUIContent(target.ToString()), typeSetting.CanDraw(target), OnChangeFlags, target);
            }

            void OnChangeFlags(object userData)
            {
                if(userData is DrawedProperty target)
                {
                    bool hasFlag = typeSetting.CanDraw(target);
                    if(hasFlag)
                    {
                        typeSetting.DrawedProperty &= ~target;
                    }
                    else
                    {
                        typeSetting.DrawedProperty |= target;
                    }

                    BroEditorUtility.EditorSetting.WriteAudioTypeSetting(typeSetting.AudioType, typeSetting);
                }
            }

#if BroAudio_DevOnly
            void CopyID()
            {
                EditorGUIUtility.systemCopyBuffer = idProp.intValue.ToString();
            } 
#endif
        }
    }
}