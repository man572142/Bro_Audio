using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Ami.Extension;
using Ami.BroAudio.Data;
using Ami.BroAudio.Editor.Setting;
using System;
using Ami.BroAudio.Tools;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Editor.BroEditorUtility;

#if PACKAGE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace Ami.BroAudio.Editor
{
	public class ReorderableClips
	{
        private struct NoLoopChainedPlayModeInfo
        {
            public GUIContent ApplyDefaultGUIContent;
            public Action OnSetDefaultChainedPlayModeLoopSettings;
            public string Message;
        }
        
        private struct HeaderInfo
        {
            public string Message;
            public MessageType MessageType;
            public GUIContent ButtonText;
            public Action OnClick;
        }

        private const MulticlipsPlayMode DefaultMulticlipsMode = MulticlipsPlayMode.Random;
		private const float Gap = 5f;
		private const float HeaderLabelWidth = 50f;
		private const float MulticlipsValueLabelWidth = 60f;
		private const float MulticlipsValueFieldWidth = 40f;
		private const float SliderLabelWidth = 25;
		private const float ObjectPickerRatio = 0.6f;
        
        private static Dictionary<int, int> _selectedClipIndexCache = new Dictionary<int, int>();
        
        public Action<string> OnClipChanged;
        
		private ReorderableList _reorderableList;
		private SerializedProperty _entityProp;
		private SerializedProperty _playModeProp;
        private SerializedProperty _useAddressablesProp;
		private int _currSelectedClipIndex = -1;
		private SerializedProperty _currSelectedClip;
		private string _currentPlayingClipPath;
        private readonly BroInstructionHelper _instruction = new BroInstructionHelper();
		private readonly GUIContent _weightGUIContent = new GUIContent("Weight", "Probability = Weight / Total Weight");
        private readonly NoLoopChainedPlayModeInfo _noLoopChainedPlayModeInfo;
#if PACKAGE_ADDRESSABLES
        private List<AudioClip> _assetReferenceCachedClips = new List<AudioClip>();  
#endif
        private RequestClipPreview _onRequestClipPreview;

        private Vector2 PlayButtonSize => new Vector2(30f, 20f);
        public bool IsMulticlips => _reorderableList.count > 1;
		public float Height => _reorderableList.GetHeight() + (HasHeaderMessage(out _) ? HeaderMessageHeight : 0f);
		public bool IsPlaying => _currentPlayingClipPath != null;
        public bool HasAnyAudioClip { get; private set; }
        public bool HasAnyAddressableClip { get; private set; }
        public SerializedProperty CurrentPlayingClip { get; private set; }
        public Rect PreviewRect { get; set; }
        private MulticlipsPlayMode CurrentPlayMode => (MulticlipsPlayMode)_playModeProp.enumValueIndex;
        private static float HeaderMessageHeight => EditorGUIUtility.singleLineHeight + 3f;

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

		public ReorderableClips(SerializedProperty entityProperty, RequestClipPreview onRequestClipPreview)
		{
			_entityProp = entityProperty;
			_playModeProp = entityProperty.FindPropertyRelative(AudioEntity.EditorPropertyName.MulticlipsPlayMode);
#if PACKAGE_ADDRESSABLES
            _useAddressablesProp = entityProperty.FindPropertyRelative(nameof(AudioEntity.UseAddressables)); 
#endif
            _reorderableList = CreateReorderableList(entityProperty);
            _onRequestClipPreview = onRequestClipPreview;
			UpdatePlayModeAndRequiredClipCount();

            var setting = BroEditorUtility.RuntimeSetting;
            float transitionTime = setting.DefaultChainedPlayModeLoop == LoopType.Loop ? 0f : setting.DefaultChainedPlayModeTransitionTime;
            _noLoopChainedPlayModeInfo = new NoLoopChainedPlayModeInfo()
            {
                Message = string.Format(_instruction.GetText(Instruction.LibraryManager_NoLoopForChainedPlayMode), setting.DefaultChainedPlayModeLoop, transitionTime),
                ApplyDefaultGUIContent = new GUIContent("Apply Defaults", _instruction.GetText(Instruction.LibraryManager_ApplyDefaultLoopForChainedPlayMode)),
                OnSetDefaultChainedPlayModeLoopSettings = SetDefaultChainedPlayModeLoopSettings
            };

			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

        public void Dispose()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
			_currentPlayingClipPath = null;
        }

		public void SelectAndSetPlayingElement(int index)
		{
			if(index >= 0)
			{
				_reorderableList.index = index;
                SetPlayingClip(_reorderableList.serializedProperty.GetArrayElementAtIndex(index).propertyPath);
			}
		}

		public void SetPlayingClip(string clipPath)
		{
            _currentPlayingClipPath = clipPath;
            CurrentPlayingClip = clipPath != null ? _entityProp.serializedObject.FindProperty(clipPath) : null;
        }

        public bool TryGetSelectedAudioClip(out AudioClip audioClip)
        {
            audioClip = null;
            if (CurrentSelectedClip == null)
            {
                return false;
            }

#if PACKAGE_ADDRESSABLES
            bool useAddressable = _entityProp.FindPropertyRelative(nameof(AudioEntity.UseAddressables)).boolValue;
            int index = _reorderableList.index;
            if (useAddressable && index >= 0 && index < _assetReferenceCachedClips.Count)
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
            if (HasHeaderMessage(out var headerInfo))
            {
                var helpBoxRect = new Rect(position) { height = HeaderMessageHeight };
                EditorGUI.HelpBox(helpBoxRect, headerInfo.Message, headerInfo.MessageType);
                DrawHeaderButton(headerInfo, helpBoxRect);
                var listRect = new Rect(position) { height = position.height - helpBoxRect.height, y = helpBoxRect.yMax - 1f };
                _reorderableList.DoList(listRect);
            }
            else
            {
                _reorderableList.DoList(position);
            }
		}

        private ReorderableList CreateReorderableList(SerializedProperty entityProperty)
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
            
            var id = entityProperty.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
            list.index = _selectedClipIndexCache.TryGetValue(id, out int index) ? index : 0;
			return list;
		}

		private void UpdatePlayModeAndRequiredClipCount()
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
                    UpdatePlayModeAndRequiredClipCount();
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
            
            var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
            EditorGUI.BeginDisabledGroup(index > 0 && index >= playMode.GetMaxAcceptableClipCount());
            {
                DrawPlayClipButton();
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
                    if (!TryGetAudioClip(out _, out ReferenceType referenceType))
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
                bool isPlaying = string.Equals(_currentPlayingClipPath, clipProp.propertyPath);
                var image = GetPlaybackButtonIcon(isPlaying).image;
                GUIContent buttonGUIContent = new GUIContent(image, EditorAudioPreviewer.IgnoreSettingTooltip);
                if (GUI.Button(buttonRect, buttonGUIContent))
                {
                    if (isPlaying)
                    {
                        EditorAudioPreviewer.Instance.StopAllClips();
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
                var currentEvent = Event.current;
                PreviewRequest req;
                if (currentEvent.button == 0) // Left Click
                {
                    var transport = new SerializedTransport(clipProp, audioClip.length);
                    req = currentEvent.CreatePreviewRequest(audioClip, volProp.floatValue, transport);
                    GetBaseAndRandomValue(RandomFlag.Volume, _entityProp, out req.BaseMasterVolume, out req.MasterVolume);
                    GetBaseAndRandomValue(RandomFlag.Pitch, _entityProp, out req.BasePitch, out req.Pitch);
                }
                else
                {
                    req = currentEvent.CreatePreviewRequest(audioClip);
                }

                _onRequestClipPreview?.Invoke(clipProp.propertyPath, req);
                _currentPlayingClipPath = clipProp.propertyPath;
                EditorAudioPreviewer.Instance.PlaybackIndicator.SetClipInfo(PreviewRect, req);
            }

			void DrawVolumeSlider()
			{
                Rect labelRect = new Rect(rect) { width = SliderLabelWidth , x = clipRect.xMax + Gap};
                Rect sliderRect = new Rect(rect) { width = (remainWidth * (1 - ObjectPickerRatio)) - Gap - SliderLabelWidth, x = labelRect.xMax};
                sliderRect.y += 2f;
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
                    case MulticlipsPlayMode.Chained:
                        EditorGUI.LabelField(valueRect, GetChainedModeText(index), GUIStyleHelper.MiddleCenterText);
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
			UpdatePlayModeAndRequiredClipCount();
		}

		private void OnAdd(ReorderableList list)
        {
			AddClip(list);
            UpdatePlayModeAndRequiredClipCount();
        }

        private void OnSelect(ReorderableList list)
        {
            var id = _entityProp.FindBackingFieldProperty(nameof(AudioEntity.ID)).intValue;
            _selectedClipIndexCache[id] = list.index;
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
        
        private static string GetChainedModeText(int index) => index switch
        {
            0 => "Start",
            1 => "Loop",
            2 => "End",
            _ => "<color=#FF3D3D>x</color>",
        };

        private bool HasHeaderMessage(out HeaderInfo headerInfo)
        {
            headerInfo = default;
            var playMode = (MulticlipsPlayMode)_playModeProp.enumValueIndex;
            if (playMode == MulticlipsPlayMode.Chained &&
                BroEditorUtility.EditorSetting.ShowWarningWhenEntityHasNoLoopInChainedMode &&
                !IsLoopBy(nameof(AudioEntity.Loop)) && !IsLoopBy(nameof(AudioEntity.SeamlessLoop)))
            {
                headerInfo = GetHeaderInfo(_noLoopChainedPlayModeInfo);
                return true;
            }
            return false;

            bool IsLoopBy(string propPath) => _entityProp.FindBackingFieldProperty(propPath).boolValue;
        }

        private void SetDefaultChainedPlayModeLoopSettings()
        {
            var setting = BroEditorUtility.RuntimeSetting;
            switch (setting.DefaultChainedPlayModeLoop)
            {
                case LoopType.Loop:
                    _entityProp.FindBackingFieldProperty(nameof(AudioEntity.Loop)).boolValue = true;
                    break;
                case LoopType.SeamlessLoop:
                    _entityProp.FindBackingFieldProperty(nameof(AudioEntity.SeamlessLoop)).boolValue = true;
                    _entityProp.FindBackingFieldProperty(nameof(AudioEntity.TransitionTime)).floatValue =
                        setting.DefaultChainedPlayModeTransitionTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _entityProp.serializedObject.ApplyModifiedProperties();
        }

        private static HeaderInfo GetHeaderInfo(NoLoopChainedPlayModeInfo info) => new HeaderInfo()
        {
            Message = info.Message,
            MessageType = MessageType.Warning,
            ButtonText = info.ApplyDefaultGUIContent, 
            OnClick = info.OnSetDefaultChainedPlayModeLoopSettings
        };
        
        private static void DrawHeaderButton(HeaderInfo headerInfo, Rect headerRect)
        {
            if (headerInfo.OnClick == null)
            {
                return;
            }
            
            var buttonSize = EditorStyles.miniButton.CalcSize(headerInfo.ButtonText);
            var buttonRect = new Rect(headerRect) { size = buttonSize, x = headerRect.xMax - buttonSize.x };
            if (GUI.Button(buttonRect, headerInfo.ButtonText))
            {
                headerInfo.OnClick.Invoke();
            }
        }
    }
}