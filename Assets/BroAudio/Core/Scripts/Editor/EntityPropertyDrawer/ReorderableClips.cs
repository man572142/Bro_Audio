using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;
using Ami.BroAudio.Tools;
using System.Collections.Generic;
using static Ami.BroAudio.Editor.BroEditorUtility;

#if PACKAGE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Ami.BroAudio.Editor
{
    public class ReorderableClips
    {
        public Action<string> OnClipChanged;

        public const MulticlipsPlayMode DefaultMulticlipsMode = MulticlipsPlayMode.Random;
        private const float Gap = 5f;
        private const float HeaderLabelWidth = 50f;
        private const float MulticlipsValueLabelWidth = 60f;
        private const float MulticlipsValueFieldWidth = 40f;
        private const float SliderLabelWidth = 25;
        private const float ObjectPickerRatio = 0.6f;

        private readonly ReorderableList _reorderableList;
        private readonly SerializedProperty _entityProp;
        private readonly SerializedProperty _playModeProp;
        private readonly SerializedProperty _useAddressablesProp;
        private readonly IEditorPreviewable _previewable;
        private int _currSelectedClipIndex = -1;
        private SerializedProperty _currSelectedClip;
        private Rect _previewRect = default;
        private GUIContent _weightGUIContent = new GUIContent("Weight", "Probability = Weight / Total Weight");
#if PACKAGE_ADDRESSABLES
        private List<AudioClip> _assetReferenceCachedClips = new List<AudioClip>();  
#endif

        private Vector2 PlayButtonSize => new Vector2(30f, 20f);
        public bool IsMulticlips => _reorderableList.count > 1;
        public float Height => _reorderableList.GetHeight();
        public Rect PreviewRect => _previewRect;
        public bool HasAnyAudioClip { get; private set; }
        public bool HasAnyAddressableClip { get; private set; }

        public SerializedProperty CurrentSelectedClip
        {
            get
            {
                if(_reorderableList.count > 0)
                {
                    if(_reorderableList.index < 0)
                    {
                        _reorderableList.index = 0;
                    }

                    if (_currSelectedClipIndex != _reorderableList.index)
                    {
                        _currSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(_reorderableList.index);
                        _currSelectedClipIndex = _reorderableList.index;
                    }
                    else if (_currSelectedClip == null)
                    {
                        _currSelectedClip = _reorderableList.serializedProperty.GetArrayElementAtIndex(_reorderableList.index);
                    }
                }
                else
                {
                    _currSelectedClip = null;
                }
                return _currSelectedClip;
            }
        }

        public ReorderableClips(SerializedProperty entityProperty, IEditorPreviewable previewable)
        {
            _entityProp = entityProperty;
            _previewable = previewable;
            _playModeProp = entityProperty.FindPropertyRelative(AudioEntity.EditorPropertyName.MulticlipsPlayMode);
#if PACKAGE_ADDRESSABLES
            _useAddressablesProp = entityProperty.FindPropertyRelative(nameof(AudioEntity.UseAddressables)); 
#endif
            _reorderableList = CreateReorderabeList(entityProperty);
            UpdatePlayMode();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public void Dispose()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public void SetPreviewRect(Rect rect)
        {
            _previewRect = rect;
        }

        public SerializedProperty SelectAndSetPlayingElement(int index)
        {
            if(index >= 0)
            {
                _reorderableList.index = index;
                var clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
                return clipProp;
            }
            return null;
        }

        public bool TryGetSelectedAudioClip(out AudioClip audioClip)
        {
            audioClip = null;
            if (CurrentSelectedClip == null)
            {
                return false;
            }

#if PACKAGE_ADDRESSABLES
            int index = _reorderableList.index;
            if (_useAddressablesProp.boolValue && index >= 0 && index < _assetReferenceCachedClips.Count)
            {
                audioClip = _assetReferenceCachedClips[index];
                return audioClip != null;
            }
#endif
            return CurrentSelectedClip.TryGetPropertyObject(BroAudioClip.NameOf.AudioClip, out audioClip);
        }

#if PACKAGE_ADDRESSABLES
        public void CleanupAllReferences(ReferenceType referenceType)
        {
            var property = _reorderableList.serializedProperty;
            for (int i = 0; i < _reorderableList.count; i++)
            {
                var clipProp = property.GetArrayElementAtIndex(i);
                switch (referenceType)
                {
                    case ReferenceType.Direct:
                        clipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip).objectReferenceValue = null;
                        break;
                    case ReferenceType.Addressalbes:
                        var assetRefProp = clipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClipAssetReference);
                        int targetDepth = assetRefProp.depth + 1;
                        while(assetRefProp.NextVisible(true) && assetRefProp.depth == targetDepth)
                        {
                            if(assetRefProp.propertyType == SerializedPropertyType.String)
                            {
                                assetRefProp.stringValue = string.Empty;
                            }
                        }
                        break;
                }
            }

            switch (referenceType)
            {
                case ReferenceType.Direct:
                    HasAnyAudioClip = false;
                    break;
                case ReferenceType.Addressalbes:
                    HasAnyAddressableClip = false;
                    _assetReferenceCachedClips.Clear();
                    break;
            }
            property.serializedObject.ApplyModifiedProperties();
        }

        public void ConvertReferences(ReferenceType referenceType, bool needSetAddressable = true)
        {
            var property = _reorderableList.serializedProperty;
            for (int i = 0; i < _reorderableList.count; i++)
            {
                var clipProp = property.GetArrayElementAtIndex(i);
                var directRefProp = clipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip);
                var assetRefGuidProp = clipProp
                    .FindPropertyRelative(BroAudioClip.NameOf.AudioClipAssetReference)
                    .FindPropertyRelative(AssetReferenceGUIDFieldName);
                string path;
                switch (referenceType)
                {
                    case ReferenceType.Direct:
                        if (directRefProp.objectReferenceValue != null)
                        {
                            path = AssetDatabase.GetAssetPath(directRefProp.objectReferenceValue);
                            string guid = AssetDatabase.AssetPathToGUID(path);
                            assetRefGuidProp.stringValue = guid;
                            directRefProp.objectReferenceValue = null;
                            if(needSetAddressable)
                            {
                                var settings = GetAddressableSettings();
                                settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                            }
                        }
                        break;
                    case ReferenceType.Addressalbes:
                        if(!string.IsNullOrEmpty(assetRefGuidProp.stringValue))
                        {
                            path = AssetDatabase.GUIDToAssetPath(assetRefGuidProp.stringValue);
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                            directRefProp.objectReferenceValue = obj;
                            if(needSetAddressable)
                            {
                                GetAddressableSettings().RemoveAssetEntry(assetRefGuidProp.stringValue);
                            }
                            assetRefGuidProp.stringValue = string.Empty;
                        }
                        break;
                }
            }

            AddressableAssetSettings GetAddressableSettings() => AddressableAssetSettingsDefaultObject.Settings;
        }
#endif

        private void OnUndoRedoPerformed()
        {
            _reorderableList.serializedProperty.serializedObject.Update();
            int count = _reorderableList.count;
            if(count == _reorderableList.index || count == _currSelectedClipIndex)
            {
                _currSelectedClipIndex = count - 1;
                _reorderableList.index = count - 1;
                _currSelectedClip = null;
            }
        }

        public void DrawReorderableList(Rect position)
        {
            _reorderableList.DoList(position);
        }

        private ReorderableList CreateReorderabeList(SerializedProperty entityProperty)
        {
            SerializedProperty clipsProp = entityProperty.FindPropertyRelative(nameof(AudioEntity.Clips));
            var list = new ReorderableList(clipsProp.serializedObject, clipsProp) 
            {
                drawHeaderCallback = OnDrawHeader,
                drawElementCallback = OnDrawElement,
                drawFooterCallback = OnDrawFooter,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                onSelectCallback = OnSelect,
            };
            if(list.count > 0)
            {
                list.index = 0;
            }
            return list;
        }

        private void UpdatePlayMode()
        {
            if (!IsMulticlips)
            {
                _playModeProp.enumValueIndex = 0;
            }
            else if (IsMulticlips && _playModeProp.enumValueIndex == 0)
            {
                _playModeProp.enumValueIndex = (int)DefaultMulticlipsMode;
            }
        }

        private void HandleClipsDragAndDrop(Rect rect)
        {
            EventType currType = Event.current.type;
            if((currType == EventType.DragUpdated || currType == EventType.DragPerform) && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if(currType == EventType.DragPerform && DragAndDrop.objectReferences?.Length > 0)
                {
                    foreach(var clipObj in DragAndDrop.objectReferences)
                    {
                        SerializedProperty broClipProp = AddClip(_reorderableList);
                        SerializedProperty audioClipProp = broClipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip);
                        audioClipProp.objectReferenceValue = clipObj;
                    }
                    UpdatePlayMode();
                    _reorderableList.serializedProperty.serializedObject.ApplyModifiedProperties();
                    DragAndDrop.AcceptDrag();
                }
            }
        }

        #region ReorderableList Callback
        private void OnDrawHeader(Rect rect)
        {
            HandleClipsDragAndDrop(rect);

            Rect labelRect = new Rect(rect) { width = HeaderLabelWidth };
            Rect valueRect = new Rect(rect) { width = MulticlipsValueLabelWidth , x = rect.xMax - MulticlipsValueLabelWidth};
            Rect remainRect = new Rect(rect) { width = (rect.width - HeaderLabelWidth - MulticlipsValueLabelWidth), x = labelRect.xMax };
            EditorScriptingExtension.SplitRectHorizontal(remainRect, 0.5f, 10f, out var multiclipOptionRect, out var masterVolRect);

            EditorGUI.LabelField(labelRect, "Clips");
            if (IsMulticlips)
            {
                var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
                playMode = (MulticlipsPlayMode)EditorGUI.EnumPopup(multiclipOptionRect, playMode);
                _playModeProp.enumValueIndex = (int)playMode;

                DrawMasterVolume(masterVolRect);

                GUIContent guiContent = new GUIContent(string.Empty);
                switch (playMode)
                {
                    case MulticlipsPlayMode.Single:
                        guiContent.tooltip = "Always play the first clip";
                        break;
                    case MulticlipsPlayMode.Sequence:
                        EditorGUI.LabelField(valueRect, "Index", GUIStyleHelper.MiddleCenterText);
                        guiContent.tooltip = "Plays the next clip each time";
                        break;
                    case MulticlipsPlayMode.Random:
                        EditorGUI.LabelField(valueRect, _weightGUIContent, GUIStyleHelper.MiddleCenterText);
                        guiContent.tooltip = "Plays a clip randomly";
                        break;
                    case MulticlipsPlayMode.Shuffle:
                        guiContent.tooltip = "Plays a clip randomly without repeating the previous one.";
                        break;
                    case MulticlipsPlayMode.Velocity:
                        EditorGUI.LabelField(valueRect, "Velocity", GUIStyleHelper.MiddleCenterText);
                        guiContent.tooltip = "Plays a clip by a given velocity";
                        break;
                }
                EditorGUI.LabelField(multiclipOptionRect.DissolveHorizontal(0.5f), "(PlayMode)".SetColor(Color.gray), GUIStyleHelper.MiddleCenterRichText);
                EditorGUI.LabelField(multiclipOptionRect, guiContent);
            }
        }

        private void DrawMasterVolume(Rect masterVolRect)
        {
            int id = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
            var editorSetting = BroEditorUtility.EditorSetting;
            if (!editorSetting.ShowMasterVolumeOnClipListHeader 
                || !editorSetting.TryGetAudioTypeSetting(Utility.GetAudioType(id), out var typeSetting) 
                || !typeSetting.CanDraw(DrawedProperty.MasterVolume))
            {
                return;
            }

            var masterProp = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.MasterVolume));
            var masterRandProp = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.VolumeRandomRange));
            float masterVol = masterProp.floatValue;
            float masterVolRand = masterRandProp.floatValue;
            RandomFlag flags = (RandomFlag)_entityProp.FindBackingFieldProperty(nameof(AudioEntity.RandomFlags)).intValue;
            GetMixerMinMaxVolume(out float minVol, out float maxVol);
            Rect masterVolLabelRect = new Rect(masterVolRect) { width = SliderLabelWidth };
            Rect masterVolSldierRect = new Rect(masterVolRect) { width = masterVolRect.width - SliderLabelWidth, x = masterVolLabelRect.xMax };

            EditorGUI.LabelField(masterVolLabelRect, EditorGUIUtility.IconContent(IconConstant.AudioSpeakerOn));
            if (flags.Contains(RandomFlag.Volume))
            {
                DrawRandomRangeSlider(masterVolSldierRect, GUIContent.none, ref masterVol, ref masterVolRand, minVol, maxVol, SliderType.BroVolumeNoField);
            }
            else
            {
                masterVol = DrawVolumeSlider(masterVolSldierRect, masterVol, out _, out float newSliderInFullScale);
                DrawDecibelValuePeeking(masterVol, 3f, masterVolRect, newSliderInFullScale);
            }
            masterProp.floatValue = masterVol;
            masterRandProp.floatValue = masterVolRand;
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty clipProp = _reorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty audioClipProp = clipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClip);
            SerializedProperty assetReferenceProp = null;
#if PACKAGE_ADDRESSABLES
            assetReferenceProp = clipProp.FindPropertyRelative(BroAudioClip.NameOf.AudioClipAssetReference);
#endif
            SerializedProperty volProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Volume));
            bool isUsingAddressable = _useAddressablesProp != null && _useAddressablesProp.boolValue && assetReferenceProp != null;

            Rect buttonRect = new Rect(rect) { width = PlayButtonSize.x, height = PlayButtonSize.y };
            buttonRect.y += (_reorderableList.elementHeight - PlayButtonSize.y) * 0.5f;
            Rect valueRect = new Rect(rect) { width = MulticlipsValueLabelWidth, x = rect.xMax - MulticlipsValueLabelWidth };

            float remainWidth = rect.width - buttonRect.width - valueRect.width;
            Rect clipRect = new Rect(rect) { width = (remainWidth * ObjectPickerRatio) - Gap, x = buttonRect.xMax + Gap};
            Rect sliderRect = new Rect(rect) { width = (remainWidth * (1 - ObjectPickerRatio)) - Gap, x = clipRect.xMax + Gap};

            DrawPlayClipButton();
            bool isSingleMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex == MulticlipsPlayMode.Single;
            EditorGUI.BeginDisabledGroup(isSingleMode && index > 0);
            {
                DrawObjectPicker();
                DrawVolumeSlider();
                DrawMulticlipsValue();
            }
            EditorGUI.EndDisabledGroup();

            void DrawObjectPicker()
            {
                EditorGUI.BeginChangeCheck();
                if(isUsingAddressable)
                {
                    EditorGUI.PropertyField(clipRect, assetReferenceProp, GUIContent.none);
                }
                else
                {
                    EditorGUI.PropertyField(clipRect, audioClipProp, GUIContent.none);
                }
                
                if (EditorGUI.EndChangeCheck())
                {
                    ResetBroClipPlaybackSetting(clipProp);
                    OnClipChanged?.Invoke(clipProp.propertyPath);
                    if (!TryGetAudioClip(out AudioClip audioClip, out ReferenceType referenceType))
                    {
                        SetHasAny(false, referenceType);
                    }
                }
            }

            void DrawPlayClipButton()
            {
                if(!TryGetAudioClip(out AudioClip audioClip, out ReferenceType referenceType))
                {
                    return;
                }

                SetHasAny(true, referenceType);
                bool isPlaying = string.Equals(_previewable.CurrentClipPath, clipProp.propertyPath);
                var image = GetPlaybackButtonIcon(isPlaying).image;
                GUIContent buttonGUIContent = new GUIContent(image, EditorPlayAudioClip.IgnoreSettingTooltip);
                if (GUI.Button(buttonRect, buttonGUIContent))
                {
                    if (isPlaying)
                    {
                        EditorPlayAudioClip.Instance.StopAllClips();
                    }
                    else
                    {
                        PreviewAudio(audioClip);
                    }
                }
            }

            bool TryGetAudioClip(out AudioClip audioClip, out ReferenceType referenceType)
            {
                audioClip = null;
                referenceType = isUsingAddressable ? ReferenceType.Addressalbes : ReferenceType.Direct;
                if(isUsingAddressable)
                {
#if PACKAGE_ADDRESSABLES
                    while (index >= _assetReferenceCachedClips.Count)
                    {
                        _assetReferenceCachedClips.Add(null);
                    }

                    audioClip = _assetReferenceCachedClips[index];
                    if (audioClip == null)
                    {
                        string guid = assetReferenceProp.FindPropertyRelative(AssetReferenceGUIDFieldName).stringValue;
                        if (!string.IsNullOrEmpty(guid))
                        {
                            audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                        }
                    }
                    _assetReferenceCachedClips[index] = audioClip; 
#endif
                }
                else
                {
                    audioClip = audioClipProp.objectReferenceValue as AudioClip;
                }
                return audioClip != null;
            }

            void PreviewAudio(AudioClip audioClip)
            {
                PreviewClipInfo info;
                float pitch = 1f;
                EditorPlayAudioClip.Instance.OnFinished = null;
                if (Event.current.button == 0) // Left Click
                {
                    _previewable.StartPreview(clipProp.propertyPath, out float volume, out pitch);
                    var transport = new SerializedTransport(clipProp, audioClip.length);
                    var clipData = new PreviewData(audioClip, volume, pitch, transport);
                    EditorPlayAudioClip.Instance.PlayClipByAudioSource(clipData);
                    info = new PreviewClipInfo(transport);
                }
                else
                {
                    EditorPlayAudioClip.Instance.PlayClip(audioClip, 0f, 0f);
                    info = new PreviewClipInfo(audioClip.length);
                }

                EditorPlayAudioClip.Instance.OnFinished = _previewable.EndPreview;
                EditorPlayAudioClip.Instance.PlaybackIndicator.SetClipInfo(_previewRect, info, pitch);
            }

            void DrawVolumeSlider()
            {
                Rect labelRect = new Rect(sliderRect) { width = SliderLabelWidth };
                sliderRect.width -= SliderLabelWidth;
                sliderRect.x = labelRect.xMax;
                EditorGUI.LabelField(labelRect, EditorGUIUtility.IconContent(IconConstant.AudioSpeakerOn));
                float newVol = BroEditorUtility.DrawVolumeSlider(sliderRect, volProp.floatValue, out bool hasChanged, out float newSliderValue);
                if (hasChanged)
                {
                    volProp.floatValue = newVol;	
                }
                DrawDecibelValuePeeking(volProp.floatValue, 3f, sliderRect, newSliderValue);
            }

            void DrawMulticlipsValue()
            {
                valueRect.width = MulticlipsValueFieldWidth;
                valueRect.x += (MulticlipsValueLabelWidth - MulticlipsValueFieldWidth) * 0.5f;
                MulticlipsPlayMode currentPlayMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
                switch (currentPlayMode)
                {
                    case MulticlipsPlayMode.Sequence:
                        EditorGUI.LabelField(valueRect, index.ToString(), GUIStyleHelper.MiddleCenterText);
                        break;
                    case MulticlipsPlayMode.Random:
                    case MulticlipsPlayMode.Velocity:
                        SerializedProperty weightProp = clipProp.FindPropertyRelative(nameof(BroAudioClip.Weight));
                        GUIStyle intFieldStyle = new GUIStyle(EditorStyles.numberField);
                        intFieldStyle.alignment = TextAnchor.MiddleCenter;
                        weightProp.intValue = EditorGUI.IntField(valueRect, weightProp.intValue, intFieldStyle);
                        break;
                }
            }
        }

        private void OnDrawFooter(Rect rect)
        {
            ReorderableList.defaultBehaviours.DrawFooter(rect, _reorderableList);
            if (TryGetSelectedAudioClip(out AudioClip audioClip))
            {
                EditorGUI.LabelField(rect, audioClip.name.SetColor(BroAudioGUISetting.ClipLabelColor).ToBold(), GUIStyleHelper.RichText);
            }
        }

        private void OnRemove(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            UpdatePlayMode();
        }

        private void OnAdd(ReorderableList list)
        {
            AddClip(list);
            UpdatePlayMode();
        }

        private void OnSelect(ReorderableList list)
        {
            EditorPlayAudioClip.Instance.StopAllClips();
        }

        private void SetHasAny(bool state, ReferenceType type)
        {
            switch (type)
            {
                case ReferenceType.Direct:
                    HasAnyAudioClip = state;
                    break;
                case ReferenceType.Addressalbes:
                    HasAnyAddressableClip = state;
                    break;
            }
        }

        #endregion

        private SerializedProperty AddClip(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var clipProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
            ResetBroAudioClipSerializedProperties(clipProp);
            return clipProp;
        }
    }
}
