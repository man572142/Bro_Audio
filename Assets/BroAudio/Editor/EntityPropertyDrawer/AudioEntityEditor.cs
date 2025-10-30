using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using System;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.Editor.BroEditorUtility;
using Decision = Ami.BroAudio.Editor.EditorSetting.ReferenceConversionDecision;
using System.IO;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(AudioEntity))]
    public partial class AudioEntityEditor : MiEditor
    {
        private class ClipData
        {
            public ITransport Transport;
            public bool IsSnapToFullVolume;
        }

        private class EntityData
        {
            public bool IsExpanded;
            public Tab SelectedTab;
            public bool IsReplay;
            public readonly Rect[] HiddenButtonRects = new Rect[4];
            public readonly SerializedObject Entity;
            
            public bool IsPlaying => Clips != null && Clips.IsPlaying;

            public ReorderableClips Clips { get; private set; }

            public EntityData(ReorderableClips clips, SerializedObject serializedObject)
            {
                Clips = clips;
                Entity = serializedObject;
            }

            public void Dispose()
            {
                Clips?.Dispose();
                Clips = null;
            }

            public void UpdateHiddenButtonRect(TransportType transportType, Rect rect)
            {
                int typeIndex = (int)transportType;
                if (typeIndex < 4)
                {
                    HiddenButtonRects[typeIndex] = rect;
                }
            }
        }

        private enum Tab { Clips, Overall }

        public static event Action<AudioEntityEditor> OnRemoveEntity;
        public static event Action<AudioEntityEditor> OnDuplicateEntity;
        public static event Action<bool> OnExpandAll;

        private const float ClipPreviewHeight = 100f;
        private const float DefaultFieldRatio = 0.9f;
        private const float PreviewPrettinessOffsetY = 7f; // for prettiness
        private const float FoldoutArrowWidth = 15f;
        private const float MaxTextFieldWidth = 300f;

        private readonly GUIContent _volumeLabel = new GUIContent(nameof(BroAudioClip.Volume), "The playback volume of this clip");
        private readonly BroInstructionHelper _instruction = new BroInstructionHelper();
        private TabViewData[] _tabViewDatas = new TabViewData[3];

        private readonly DrawClipPropertiesHelper _clipPropHelper = new DrawClipPropertiesHelper();
        private readonly Dictionary<string, ClipData> _clipDataDict = new Dictionary<string, ClipData>();
        private readonly Dictionary<AudioEntity, EntityData> _entityDataDict = new Dictionary<AudioEntity, EntityData>();
        private AudioEntity _currentPreviewingEntity;
        private KeyValuePair<string, PreviewRequest> _currentPreviewRequest;

        public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
        private float TabLabelHeight => SingleLineSpace * 1.3f;
        private float TabLabelCompensation => SingleLineSpace * 2 - TabLabelHeight;
        
        private static EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
        private static float ClipPreviewPadding => ReorderableList.Defaults.padding;
        private static float SnapVolumePadding => ReorderableList.Defaults.padding;

        protected override void OnEnable()
        {
            _tabViewDatas = new TabViewData[]
            {
                new TabViewData(0.475f, new GUIContent(nameof(Tab.Clips))),
                new TabViewData(0.475f, new GUIContent(nameof(Tab.Overall))),
                new TabViewData(0.05f, EditorGUIUtility.IconContent("pane options"), rect => OnOpenOptionMenu(rect)),
            };

            base.OnEnable();

            LibraryManagerWindow.OnCloseLibraryManagerWindow += OnDisable;
            LibraryManagerWindow.OnSelectAsset += OnDisable;
            LibraryManagerWindow.OnLostFocusEvent += OnLostFocus;
        }

        private void OnLostFocus()
        {
            ResetPreview();
        }

        private void OnDisable()
        {
            foreach (var data in _entityDataDict.Values)
            {
                data.Dispose();
            }
            _entityDataDict.Clear();
            _clipDataDict.Clear();

            LibraryManagerWindow.OnCloseLibraryManagerWindow -= OnDisable;
            LibraryManagerWindow.OnSelectAsset -= OnDisable;
            LibraryManagerWindow.OnLostFocusEvent -= OnLostFocus;

            ResetPreview();

            OnDuplicateEntity = null;
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

        #region Unity Entry Overrider
        public void DrawGUI(Rect position)
        {
            base.OnInspectorGUI();

            try
            {
                EditorGUI.BeginChangeCheck();

                Rect foldoutRect = GetRectAndIterateLine(position);
                SplitRectHorizontal(foldoutRect, 0.55f, 50f, out Rect nameRect, out Rect headerButtonRect);
                SplitRectHorizontal(headerButtonRect, 0.5f, 5f, out Rect previewButtonRect, out Rect audioTypeRect);

                GetOrCreateEntityDataDict(out var data);
                if (EditorSetting.ShowPlayButtonWhenEntityCollapsed)
                {
                    DrawEntityPreviewButton(previewButtonRect, data);
                }

                EditorGUI.BeginChangeCheck();
                data.IsExpanded = EditorGUI.Foldout(foldoutRect.AdjustWidth(-audioTypeRect.width), data.IsExpanded, data.IsExpanded ? string.Empty : target.name, !data.IsExpanded);
                if (EditorGUI.EndChangeCheck() && Event.current.alt)
                {
                    OnExpandAll?.Invoke(data.IsExpanded);
                }

                DrawAudioTypeButton(audioTypeRect, GetAudioType());
                if (!data.IsExpanded || !TryGetAudioTypeSetting(out var setting))
                {
                    return;
                }
                DrawEntityNameField(nameRect);
                if (!EditorSetting.ShowPlayButtonWhenEntityCollapsed)
                {
                    DrawEntityPreviewButton(previewButtonRect, data);
                }

                _clipPropHelper.DrawDraggableHiddenButton(data.HiddenButtonRects, setting);

                Rect tabViewRect = GetRectAndIterateLine(position).SetHeight(GetTabWindowHeight());
                data.SelectedTab = (Tab)DrawButtonTabsMixedView(tabViewRect, (int)data.SelectedTab, TabLabelHeight, _tabViewDatas);

                DrawEmptyLine(1);

                position.x += IndentInPixel;
                position.width -= IndentInPixel * 2f;

                switch (data.SelectedTab)
                {
                    case Tab.Clips:
#if PACKAGE_ADDRESSABLES
                        Offset -= SingleLineSpace * 0.5f;
                        DrawUseAddressablesToggle(position, data.Clips);
#endif
                        DrawReorderableClipsList(position, data.Clips, OnClipChanged);
                        SerializedProperty currSelectClip = data.Clips.CurrentSelectedClip;
                        if (data.Clips.TryGetSelectedAudioClip(out AudioClip audioClip))
                        {
                            DrawClipProperties(position, currSelectClip, audioClip, setting, out ITransport transport);
                            if (setting.CanDraw(DrawedProperty.ClipPreview) && audioClip != null && Event.current.type != EventType.Layout)
                            {
                                DrawEmptyLine(1);
                                Rect previewRect = GetNextLineRect(position);
                                previewRect.y -= PreviewPrettinessOffsetY;
                                previewRect.height = ClipPreviewHeight;
                                _clipPropHelper.DrawClipWaveformAndVisualEditing(previewRect, transport, audioClip, currSelectClip.propertyPath, OnPreviewRequest, DrawPlaybackValuePeeking);
                                data.Clips.PreviewRect = previewRect;
                                Offset += ClipPreviewHeight + ClipPreviewPadding;
                            }
                        }
                        break;
                    case Tab.Overall:
                        DrawAdditionalBaseProperties(position, setting);
                        break;
                }

                var evt = Event.current;
                if (evt.type == EventType.ContextClick && position.Contains(evt.mousePosition) && !tabViewRect.Contains(evt.mousePosition))
                {
                    OnOpenOptionMenu(new Rect(tabViewRect) { x = evt.mousePosition.x, height = EditorGUIUtility.singleLineHeight });
                }

                if (data.IsPlaying)
                {
                    UpdatePreview(data);
                }

                float GetTabWindowHeight()
                {
                    float height = TabLabelHeight;
                    height += GetTabViewHeight(setting, data.SelectedTab);
                    height += TabLabelCompensation;
                    return height;
                }

                void DrawPlaybackValuePeeking(ITransport transport, TransportType transportType, Rect dragPointRect)
                {
                    if (!setting.CanDraw(transportType.GetDrawedProperty()))
                    {
                        data.UpdateHiddenButtonRect(transportType, dragPointRect);
                        if (dragPointRect.Contains(Event.current.mousePosition))
                        {
                            Rect rect = new Rect(dragPointRect) { width = 50f };
                            rect.y -= dragPointRect.height;
                            rect.x -= dragPointRect.width * 0.5f;
                            DrawValuePeeking(rect, transport.GetValue(transportType).ToString("0.000"));
                        }
                    }
                }
            }
            finally
            {
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var position = EditorGUILayout.GetControlRect(false, GetHeight(), GUILayout.ExpandWidth(true));
            DrawGUI(position);
        }

        private void UpdatePreview(EntityData data)
        {
            if (_currentPreviewRequest.Value == null)
            {
                return;
            }
            
            var masterVolProp = data.Entity.FindBackingFieldProperty(nameof(AudioEntity.MasterVolume));
            var pitchProp = data.Entity.FindBackingFieldProperty(nameof(AudioEntity.Pitch));
            
            var req = _currentPreviewRequest.Value;
            req.UpdateRandomizedPreviewValue(RandomFlag.Volume, masterVolProp.floatValue);
            req.UpdateRandomizedPreviewValue(RandomFlag.Pitch, pitchProp.floatValue);
            req.ClipVolume = data.Clips.CurrentPlayingClip.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue;
            EditorAudioPreviewer.Instance.UpdatePreview();
        }

#if PACKAGE_ADDRESSABLES
        private void DrawUseAddressablesToggle(Rect position, ReorderableClips clips)
        {
            SerializedProperty useAddressablesProp = serializedObject.FindProperty(nameof(AudioEntity.UseAddressables));
            Rect rect = GetRectAndIterateLine(position);
            rect.width = 100f;
            rect.x = position.xMax - rect.width;
            EditorGUI.BeginChangeCheck();
            useAddressablesProp.boolValue = EditorGUI.ToggleLeft(rect, "Addressables", useAddressablesProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                EditorAudioPreviewer.Instance.StopAllClips();
                SwitchAddressable(useAddressablesProp, clips);
            }
        }

        private void SwitchAddressable(SerializedProperty property, ReorderableClips clips)
        {
            bool hasAny = false;
            Decision decision = default;
            ReferenceType type = property.boolValue ? ReferenceType.Direct : ReferenceType.Addressalbes;
            switch (type)
            {
                case ReferenceType.Direct:
                    hasAny = clips.HasAnyAudioClip;
                    decision = EditorSetting.DirectReferenceDecision;
                    break;
                case ReferenceType.Addressalbes:
                    hasAny = clips.HasAnyAddressableClip;
                    decision = EditorSetting.AddressableDecision;
                    break;
            }

            if (!hasAny)
            {
                return;
            }

            switch (decision)
            {
                case Decision.AlwaysAsk:
                    ShowDialogAndHandleResult(clips, type);
                    break;
                case Decision.OnlyConvert:
                    clips.ConvertReferences(type, false);
                    break;
                case Decision.ConvertAndSetAddressables:
                    clips.ConvertReferences(type);
                    break;
                case Decision.ConvertAndClearAllReferences:
                    clips.CleanupAllReferences(type);
                    break;
            }

            void ShowDialogAndHandleResult(ReorderableClips clips, ReferenceType current)
            {
                string format = _instruction.GetText(Instruction.LibraryManager_AddressableConversionDialog);
                string message = GetMessage(current, format);
                int result = EditorUtility.DisplayDialogComplex("References Conversion Confirmation", message, "Yes", "No", $"Yes, don't ask me again");
                switch (result)
                {
                    case 0: // Yes
                        clips.ConvertReferences(current);
                        break;
                    case 2: // Yes, never ask
                        SetDecision(current, Decision.ConvertAndSetAddressables);
                        clips.ConvertReferences(current);
                        break;
                    default: // No, or Cancel
                        property.boolValue = !property.boolValue; // revert the change
                        break;
                }
            }

            string GetMessage(ReferenceType currentRefType, string format)
            {
                string conversion = null;
                string action = null;
                switch (currentRefType)
                {
                    case ReferenceType.Direct:
                        conversion = "[Direct Reference] to [Asset Reference]";
                        action = "mark the asset as";
                        break;
                    case ReferenceType.Addressalbes:
                        conversion = "[Asset Reference] to [Direct Reference]";
                        action = "unmark the asset's";
                        break;
                }
                return string.Format(format, conversion, action);
            }

            void SetDecision(ReferenceType referenceType, Decision decision)
            {
                switch (referenceType)
                {
                    case ReferenceType.Direct:
                        EditorSetting.DirectReferenceDecision = decision;
                        break;
                    case ReferenceType.Addressalbes:
                        EditorSetting.AddressableDecision = decision;
                        break;
                }
                EditorUtility.SetDirty(EditorSetting);
            }
        }
#endif
        private void GetOrCreateEntityDataDict(out EntityData data)
        {
            if (!_entityDataDict.TryGetValue(target as AudioEntity, out data))
            {
                var reorderableClips = new ReorderableClips(serializedObject, OnPreviewRequest);
                data = new EntityData(reorderableClips, serializedObject);
                _entityDataDict[target as AudioEntity] = data;
            }
        }

        private void OnPreviewRequest(string clipPath, PreviewRequest req)
        {
            GetOrCreateEntityDataDict(out var entityData);

            GetBaseAndRandomValue(RandomFlag.Volume, entityData.Entity, out req.BaseMasterVolume, out req.MasterVolume);
            GetBaseAndRandomValue(RandomFlag.Pitch, entityData.Entity, out req.BasePitch, out req.Pitch);
            var clipProp = entityData.Entity.FindProperty(clipPath);
            req.ClipVolume = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue;
            
            EditorAudioPreviewer.Instance.Play(req);
            entityData.Clips.SetPlayingClip(clipPath);
            _currentPreviewRequest = new KeyValuePair<string, PreviewRequest>(clipPath, req);
            EditorAudioPreviewer.Instance.OnFinished = ResetPreview;
        }

        public bool IsExpanded
        {
            get
            {
                GetOrCreateEntityDataDict(out var data);
                return data.IsExpanded;
            }
            set
            {
                GetOrCreateEntityDataDict(out var data);
                data.IsExpanded = value;
            }
        }

        public float GetHeight()
        {
            float height = SingleLineSpace; // Header

            if (IsExpanded && TryGetAudioTypeSetting(out var setting))
            {
                height += ReorderableList.Defaults.padding; // reorderableList element padding;
                height += TabLabelHeight + TabLabelCompensation;

                GetOrCreateEntityDataDict(out var data);

                height += GetTabViewHeight(setting, data.SelectedTab);
            }
            return height;
        }

        private float GetClipListHeight(EditorSetting.AudioTypeSetting setting)
        {
            float height = 0f;
            GetOrCreateEntityDataDict(out var data);

            bool isShowClipProp = data.Clips.TryGetSelectedAudioClip(out _);

            height += data.Clips.Height;
            height += isShowClipProp ? GetAdditionalClipPropertiesHeight(setting) : 0f;
            height += isShowClipProp && setting.CanDraw(DrawedProperty.ClipPreview) ? ClipPreviewHeight + ClipPreviewPadding : 0f;
#if PACKAGE_ADDRESSABLES
            height += SingleLineSpace * 0.5f;
#endif

            return height;
        }

        private float GetTabViewHeight(EditorSetting.AudioTypeSetting setting, Tab tab) => tab switch
        {
            Tab.Clips => GetClipListHeight(setting),
            Tab.Overall => GetAdditionalBasePropertiesHeight(setting),
            _ => 0f,
        };
        #endregion

        private void DrawEntityNameField(Rect position)
        {
            Rect nameRect = new Rect(position) { height = EditorGUIUtility.singleLineHeight };
            nameRect.x += FoldoutArrowWidth;
            nameRect.width = Mathf.Min(nameRect.width - FoldoutArrowWidth, MaxTextFieldWidth);
            nameRect.y += 1f;

            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.LabelField(nameRect, "Multiple Objects Selected");
                return;
            }

            EditorGUI.BeginChangeCheck();
            var oldName = target.name;
            var name = EditorGUI.DelayedTextField(nameRect, GUIContent.none, oldName);
            if (EditorGUI.EndChangeCheck() && name != target.name)
            {
                serializedObject.FindProperty("m_Name").stringValue = name;
                target.name = name;

                serializedObject.ApplyModifiedProperties();

                if (AssetDatabase.Contains(target))
                {
                    var oldPath = AssetDatabase.GetAssetPath(target);

                    // attempt to move the file
                    var newPath = Path.Combine(Path.GetDirectoryName(oldPath), name + ".asset");

                    newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);

                    var error = AssetDatabase.ValidateMoveAsset(oldPath, newPath);

                    if (!string.IsNullOrEmpty(error))
                    {
                        serializedObject.FindProperty("m_Name").stringValue = name;
                        target.name = name;

                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.DisplayDialog("Move Failed", error, "OK");
                        return;
                    }

                    error = AssetDatabase.MoveAsset(oldPath, newPath);

                    if (!string.IsNullOrEmpty(error))
                    {
                        serializedObject.FindProperty("m_Name").stringValue = name;
                        target.name = name;

                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.DisplayDialog("Move Failed", error, "OK");
                        return;
                    }
                }
            }
        }

        private void DrawEntityPreviewButton(Rect rect, EntityData data)
        {
            if (!data.Clips.HasClips)
            {
                return;
            }

            rect = rect.SetHeight(h => h * 1.1f);
            SplitRectHorizontal(rect, 0.5f, 5f, out Rect playButtonRect, out Rect loopToggleRect);
            data.IsReplay = DrawButtonToggle(loopToggleRect, data.IsReplay, EditorGUIUtility.IconContent(IconConstant.LoopIcon));
            if (GUI.Button(playButtonRect, GetPlaybackButtonIcon(data.IsPlaying)) && TryGetEntityInstance(out var entity))
            {
                if (data.IsPlaying)
                {
                    EditorAudioPreviewer.Instance.StopAllClips();
                }
                else
                {
                    EntityAudioPreview(data, entity, IsExpanded);
                }
            }

            if (data.IsPlaying)
            {
                EditorAudioPreviewer.Instance.PlaybackIndicator.SetVisibility(data.SelectedTab == Tab.Clips);
            }
        }

        private void DrawAudioTypeButton(Rect position, BroAudioType audioType)
        {
            if (GUI.Button(position, string.Empty))
            {
                var menu = CreateAudioTypeGenericMenu(Instruction.LibraryManager_ChangeEntityAudioType, (audioTypeRaw) =>
                {
                    BroAudioType audioType = (BroAudioType)audioTypeRaw;
                    serializedObject.FindBackingFieldProperty(nameof(AudioEntity.AudioType)).intValue = (int)audioType;
                    serializedObject.ApplyModifiedProperties();
                });
                menu.DropDown(position);
            }
            string audioTypeName = audioType == BroAudioType.None ? "Undefined Type" : audioType.ToString();
            EditorGUI.DrawRect(position.PolarCoordinates(-1f), EditorSetting.GetAudioTypeColor(audioType));
            EditorGUI.LabelField(position, audioTypeName, GUIStyleHelper.MiddleCenterText);
        }

        private void DrawReorderableClipsList(Rect position, ReorderableClips reorderableClips, Action<string> onClipChanged)
        {
            Rect rect = GetNextLineRect(position);
            reorderableClips.DrawReorderableList(rect);
            Offset += reorderableClips.Height;
            reorderableClips.OnClipChanged = onClipChanged;
        }

        private void DrawClipProperties(Rect position, SerializedProperty clipProp, AudioClip audioClip, EditorSetting.AudioTypeSetting setting, out ITransport transport)
        {
            SerializedProperty volumeProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
            
            if (!_clipDataDict.TryGetValue(clipProp.propertyPath, out var clipData))
            {
                clipData = new ClipData { Transport = new SerializedTransport(clipProp, audioClip.length) };
                _clipDataDict[clipProp.propertyPath] = clipData;
            }
            transport = clipData.Transport;

            if (setting.CanDraw(DrawedProperty.Volume))
            {
                Rect volRect = GetRectAndIterateLine(position).SetWidth(w => w * DefaultFieldRatio);
                EditorGUI.BeginProperty(volRect, _volumeLabel, volumeProp);
                volumeProp.floatValue = DrawVolumeSlider(volRect, _volumeLabel, volumeProp.floatValue, clipData.IsSnapToFullVolume, () =>
                {
                    clipData.IsSnapToFullVolume = !clipData.IsSnapToFullVolume;
                });
                EditorGUI.EndProperty();
            }
            
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

        private bool TryGetAudioTypeSetting(out EditorSetting.AudioTypeSetting setting)
        {
            var audioType = (BroAudioType)serializedObject.FindBackingFieldProperty(nameof(AudioEntity.AudioType)).intValue;
            return EditorSetting.TryGetAudioTypeSetting(audioType, out setting);
        }

        private void OnClipChanged(string clipPropPath)
        {
            _clipDataDict.Remove(clipPropPath);
        }

        private void EntityAudioPreview(EntityData data, AudioEntity entity, bool canDisplayIndicator)
        {
            if (entity == null)
            {
                return;
            }

            _currentPreviewingEntity = entity;
            var context = entity.PlayMode == MulticlipsPlayMode.Chained ? (int)PlaybackStage.Start : 0;
            var clip = entity.PickNewClip(context, out int index);
            data.Clips.SelectAndSetPlayingElement(index);
            
            var req = new PreviewRequest(clip)
            {
                MasterVolume = entity.GetMasterVolume(),
                BaseMasterVolume = entity.MasterVolume,
                Pitch = entity.GetPitch(),
                BasePitch = entity.Pitch,
            };
            var replayReq = data.IsReplay ? new EntityReplayRequest(entity, data.Clips.SelectAndSetPlayingElement) : null;
            EditorAudioPreviewer.Instance.Play(req, replayReq);
            _currentPreviewRequest = new KeyValuePair<string, PreviewRequest>(data.Clips.CurrentSelectedClip.propertyPath, req);
            var previewRect = canDisplayIndicator ? data.Clips.PreviewRect : default;
            EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(previewRect, req);
            EditorAudioPreviewer.Instance.OnFinished = ResetPreview;
        }

        private void ResetPreview()
        {
            foreach (var entityData in _entityDataDict.Values)
            {
                entityData.Clips.SetPlayingClip(null);
            }

            _currentPreviewRequest = default;

            if (_currentPreviewingEntity != null)
            {
                _currentPreviewingEntity.ResetMultiClipStrategy();
            }
        }

        private bool TryGetEntityInstance(out AudioEntity entity)
        {
            entity = target as AudioEntity;
            return entity != null;
        }

        private BroAudioType GetAudioType() => (BroAudioType)serializedObject.FindBackingFieldProperty(nameof(AudioEntity.AudioType)).intValue;

        public void OnOpenOptionMenu(Rect? rect = null)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent($"Duplicate ^D"), false, () => OnDuplicateEntity?.Invoke(this));
            menu.AddItem(new GUIContent($"Remove _DELETE"), false, () => OnRemoveEntity?.Invoke(this));

            var audioType = GetAudioType();
            if (!BroEditorUtility.EditorSetting.TryGetAudioTypeSetting(audioType, out var typeSetting))
            {
                return;
            }

            menu.AddSeparator(string.Empty);
            menu.AddDisabledItem(new GUIContent($"Displayed properties of AudioType.{audioType}"));
            ForeachConcreteDrawedProperty(OnAddMenuItem);

            if (rect.HasValue)
            {
                menu.DropDown(rect.Value);
            }
            else
            {
                menu.ShowAsContext();
            }

            void OnAddMenuItem(DrawedProperty target)
            {
                menu.AddItem(new GUIContent(target.ToString()), typeSetting.CanDraw(target), OnChangeFlags, target);
            }

            void OnChangeFlags(object userData)
            {
                if (userData is DrawedProperty target)
                {
                    bool hasFlag = typeSetting.CanDraw(target);
                    if (hasFlag)
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
        }
    }
}