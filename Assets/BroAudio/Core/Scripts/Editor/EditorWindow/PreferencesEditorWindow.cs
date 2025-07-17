using Ami.Extension;
using Ami.Extension.Reflection;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Editor.IconConstant;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Utility;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor.Setting
{
    public class PreferencesEditorWindow : MiEditorWindow
    {
        public enum Tab { Audio, GUI, Miscellaneous,}

        public const float Gap = 50f;
        
        public const string ResetSettingButtonText = "Reset To Factory Settings";
        public const string RegenerateUserData = "Regenerate User Data";
        public const string AutoMatchTracksButtonText = "Auto-adding tracks to match audio voices.";
        public const string AssetOutputPathLabel = "Asset Output Path";
        public const string AssetOutputPathMissing = "The current audio asset output path is missing. Please select a new location.";
        public const string ShowPlayButtonWhenCollapsed = "Show play button when the entity is collapsed in Library Manager";
        public const string OpenLastEditedAssetLabel = "Open last edited asset when Library Manager launches";
        public const string VUColorToggleLabel = "Show VU color on volume slider";
        public const string ShowAudioTypeToggleLabel = "Show audioType on SoundID";
        public const string ShowMasterVolumeLabel = "Show master volume on clip list header";
        public const string AudioTypeColorLabel = "Audio Type Color";
        public const string AudioTypeDrawedProperties = "Displayed Properties";
        public const string SpectrumBandColors = "Spectrum Band Colors";
        public const string ProjectSettingsMenuItemPath = "Edit/" + ProjectSettings;
        public const string ProjectSettings = "Project Settings";

        private readonly float[] _tabLabelRatios = new float[] { 0.33f,0.33f,0.34f};

        private GUIContent _pitchGUIContent, _audioVoicesGUIContent, _virtualTracksGUIContent, _filterSlopeGUIContent, _acceptAudioMixerGUIContent
            ,_playMusicAsBgmGUIContent, _logAccessRecycledWarningGUIContent, _poolSizeCountGUIContent,_dominatorTrackGUIContent, _regenerateUserDataGUIContent
            ,_globalGroupGUIContent, _updateModeGUIContent;

#if PACKAGE_ADDRESSABLES
        private GUIContent _addressableConversionGUIContent, _directToAddressableGUIContent, _addressableToDirectGUIContent; 
#endif

        private GUIContent[] _tabLabels = null;
        private Tab _currSelectedTab = Tab.Audio;
        private int _currProjectSettingVoiceCount = -1;
        private int _currentMixerTracksCount = -1;
        private int _broVirtualTracksCount = BroAdvice.VirtualTrackCount;
        private BroInstructionHelper _instruction = new BroInstructionHelper();
        private AudioMixerGroup _duplicateTrackSource = null;
        private AudioMixer _mixer = null;
        private Vector2 _scrollPos = default;
        private float _demoSliderValue = 1f;
        private Rect[] _tabPreAllocRects = null;
        private bool _hasOutputAssetPath = false;
        private ReorderableList _spectrumColorsList = null;
        private SerializedObject _runtimeSettingSO = null;
        private SerializedObject _editorSettingSO = null;

        public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

        private EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
        private RuntimeSetting RuntimeSetting => BroEditorUtility.RuntimeSetting;

        private float TabLabelHeight => EditorGUIUtility.singleLineHeight * 2f;

        private AudioMixer AudioMixer
        {
            get
            {
                if (!_mixer)
                {
                    string[] mixerGUIDs = AssetDatabase.FindAssets(MixerName);
                    if (mixerGUIDs != null && mixerGUIDs.Length > 0)
                    {
                        _mixer = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(mixerGUIDs[0]), typeof(AudioMixer)) as AudioMixer;
                    }
                }
                return _mixer;
            }
        }

        [MenuItem(PreferencesMenuPath,false,PreferencesMenuIndex)]
        public static PreferencesEditorWindow ShowWindow()
        {
            EditorWindow window = GetWindow(typeof(PreferencesEditorWindow));
            window.minSize = new Vector2(640f, 480f);
            window.titleContent = new GUIContent(MenuItem_Preferences, EditorGUIUtility.IconContent("EditorSettings Icon").image);
            window.Show();
            return window as PreferencesEditorWindow;
        }

        private void OnEnable()
        {
            InitTabsLabel();
            InitGUIContents();
            _hasOutputAssetPath = Directory.Exists(BroEditorUtility.AssetOutputPath);
            _runtimeSettingSO = new SerializedObject(RuntimeSetting);
            _editorSettingSO = new SerializedObject(EditorSetting);

            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            ResetTracksAndAudioVoices();
            Undo.undoRedoPerformed -= Repaint;
        }

        private void InitGUIContents()
        {
            _pitchGUIContent = new GUIContent("Pitch Shift Using", _instruction.GetText(Instruction.PitchShiftingToolTip));
            _audioVoicesGUIContent = new GUIContent("Max Real Voices", _instruction.GetText(Instruction.AudioVoicesToolTip));
            _virtualTracksGUIContent = new GUIContent("Bro Virtual Tracks", _instruction.GetText(Instruction.BroVirtualToolTip));
            _globalGroupGUIContent = new GUIContent("Global Playback Group", _instruction.GetText(Instruction.GlobalPlaybackGroup));
            _filterSlopeGUIContent = new GUIContent("Audio Filter Slope", _instruction.GetText(Instruction.AudioFilterSlope));
            _acceptAudioMixerGUIContent = new GUIContent("Accept BroAudioMixer Modification");
            _playMusicAsBgmGUIContent = new GUIContent("Always Play Music As BGM", _instruction.GetText(Instruction.AlwaysPlayMusicAsBGM));
            _logAccessRecycledWarningGUIContent = new GUIContent("Log Access Recycled Player Warning", _instruction.GetText(Instruction.LogAccessRecycledWarning));
            _poolSizeCountGUIContent = new GUIContent("Audio Player Object Pool Size", _instruction.GetText(Instruction.AudioPlayerPoolSize));
            _dominatorTrackGUIContent = new GUIContent("Add Dominator Track", _instruction.GetText(Instruction.AddDominatorTrack));
            _regenerateUserDataGUIContent = new GUIContent("Regenerate User Data", _instruction.GetText(Instruction.RegenerateUserData));
            _updateModeGUIContent = new GUIContent("Update Mode", _instruction.GetText(Instruction.UpdateMode));

#if PACKAGE_ADDRESSABLES
            string aaTooltip = _instruction.GetText(Instruction.LibraryManager_AddressableConversionTooltip);
            _addressableConversionGUIContent = new GUIContent("Addressable References Conversion".ToWhiteBold(), aaTooltip);
            _directToAddressableGUIContent = new GUIContent("Direct → Addressables", aaTooltip);
            _addressableToDirectGUIContent = new GUIContent("Addressables → Direct", aaTooltip); 
#endif
        }

        private ReorderableList InitSpectrumReorderableList()
        {
            var spectrumColorsProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.SpectrumBandColors));
            return new ReorderableList(spectrumColorsProp.serializedObject, spectrumColorsProp) 
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Colors"),
                drawElementCallback = OnDrawElement,
            };

            void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                SplitRectHorizontal(rect, 0.1f, 0f, out Rect labelRect, out Rect colorRect);
                EditorGUI.LabelField(labelRect, new GUIContent(index.ToString()));

                var colorProp = _spectrumColorsList.serializedProperty.GetArrayElementAtIndex(index);
                colorProp.colorValue = EditorGUI.ColorField(colorRect, colorProp.colorValue);
            }
        }

        private void ResetTracksAndAudioVoices()
        {
            _currProjectSettingVoiceCount = -1;
            _currentMixerTracksCount = -1;
        }

        private void InitTabsLabel()
        {
            _tabLabels ??= new GUIContent[3];

            _tabLabels[(int)Tab.Audio] = EditorGUIUtility.IconContent(AudioSettingTab);
            _tabLabels[(int)Tab.GUI] = EditorGUIUtility.IconContent(GUISettingTab);
            _tabLabels[(int)Tab.Miscellaneous] = EditorGUIUtility.IconContent(CogIcon);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            _runtimeSettingSO.Update();
            _editorSettingSO.Update();

            _spectrumColorsList ??= InitSpectrumReorderableList();
            Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

            DrawEmptyLine(1);

            drawPosition.x += Gap;
            drawPosition.width -= Gap * 2;

            Rect tabWindowRect = GetRectAndIterateLine(drawPosition);
            tabWindowRect.yMax = drawPosition.yMax;
            _tabPreAllocRects ??= new Rect[_tabLabelRatios.Length];
            _currSelectedTab = (Tab)DrawTabsView(tabWindowRect, (int)_currSelectedTab, TabLabelHeight, _tabLabels, _tabLabelRatios, _tabPreAllocRects, EditorAudioPreviewer.Instance.StopAllClips);

            using (new EditorGUI.IndentLevelScope())
            {
                Rect tabPageScrollRect = new Rect(tabWindowRect.x, tabWindowRect.y + TabLabelHeight, tabWindowRect.width, tabWindowRect.height - TabLabelHeight);
                _scrollPos = BeginScrollView(tabPageScrollRect, _scrollPos);
                DrawEmptyLine(1);
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), _currSelectedTab.ToString().SetSize(18), GUIStyleHelper.MiddleCenterRichText);
                DrawEmptyLine(1);
                switch (_currSelectedTab)
                {
                    case Tab.Audio:
                        DrawAudioSetting(drawPosition);
                        break;
                    case Tab.GUI:
                        DrawGUISetting(drawPosition);
                        break;
                    case Tab.Miscellaneous:
                        DrawMiscellaneousSetting(drawPosition);
                        break;
                }
                EndScrollView();
            }

            _runtimeSettingSO.ApplyModifiedProperties();
            _editorSettingSO.ApplyModifiedProperties();
        }

        private void DrawAudioSetting(Rect drawPosition)
        {
            drawPosition.width -= Gap;
            DrawGlobalPlaybackGroup();
            DrawAudioFilterSlope();
            DrawUpdateMode();
            DrawEmptyLine(1);
            DrawBGMSetting();
            // To make a room for other functions to use exposed parameters, we only use AudioSource.pitch for now
            //DrawPitchSetting();
            DrawEmptyLine(1);
            DrawDefaultEasing();
            DrawSeamlessLoopEasing();
            DrawEmptyLine(1);
            DrawAudioPlayerSetting();
            DrawEmptyLine(1);
            DrawAudioProjectSettings();

            void DrawBGMSetting()
            {
                var alwaysBGMProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.AlwaysPlayMusicAsBGM));
                var bgmTransitionProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultBGMTransition));
                var bgmTransitionTimeProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultBGMTransitionTime));
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "BGM".ToWhiteBold(), GUIStyleHelper.RichText);
                using (new EditorGUI.IndentLevelScope())
                {
                    Rect toggleRect = GetRectAndIterateLine(drawPosition);
                    using (new LabelWidthScope(EditorGUIUtility.labelWidth * 1.3f))
                    {
                        alwaysBGMProp.boolValue = EditorGUI.Toggle(toggleRect, _playMusicAsBgmGUIContent, alwaysBGMProp.boolValue);

                        if (alwaysBGMProp.boolValue)
                        {
                            bgmTransitionProp.enumValueIndex =
                                (int)(Transition)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Default Transition", (Transition)bgmTransitionProp.enumValueIndex);

                            Rect timeRect = GetRectAndIterateLine(drawPosition);
                            timeRect.width = 250f;
                            bgmTransitionTimeProp.floatValue = 
                                EditorGUI.FloatField(timeRect, "Default Transition Time", bgmTransitionTimeProp.floatValue);
                        }
                    }
                }
            }

            void DrawDefaultEasing()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Default Easing".ToWhiteBold(), GUIStyleHelper.RichText);
                using (new EditorGUI.IndentLevelScope())
                {
                    var fadeInProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultFadeInEase));
                    var fadeOutProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultFadeOutEase));
                    fadeInProp.enumValueIndex =
                        (int)(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", (Ease)fadeInProp.enumValueIndex);
                    fadeOutProp.enumValueIndex =
                        (int)(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", (Ease)fadeOutProp.enumValueIndex);
                }
            }

            void DrawSeamlessLoopEasing()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Seamless Loop Easing".ToWhiteBold(), GUIStyleHelper.RichText);
                using (new EditorGUI.IndentLevelScope())
                {
                    var fadeInProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.SeamlessFadeInEase));
                    var fadeOutProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.SeamlessFadeOutEase));
                    fadeInProp.enumValueIndex =
                        (int)(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", (Ease)fadeInProp.enumValueIndex);
                    fadeOutProp.enumValueIndex =
                        (int)(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", (Ease)fadeOutProp.enumValueIndex);
                }
            }

            void DrawGlobalPlaybackGroup()
            {
                var playbackGroupProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.GlobalPlaybackGroup));
                playbackGroupProp.objectReferenceValue = (PlaybackGroup)EditorGUI.ObjectField(GetRectAndIterateLine(drawPosition), _globalGroupGUIContent, playbackGroupProp.objectReferenceValue, typeof(PlaybackGroup), false);
            }

            void DrawAudioFilterSlope()
            {
                var filterSlopeProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.AudioFilterSlope));
                filterSlopeProp.enumValueIndex = (int)(FilterSlope)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), _filterSlopeGUIContent, (FilterSlope)filterSlopeProp.enumValueIndex);
            }

            void DrawUpdateMode()
            {
                var updateModeProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.UpdateMode));
                updateModeProp.enumValueIndex = (int)(AudioMixerUpdateMode)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), _updateModeGUIContent, (AudioMixerUpdateMode)updateModeProp.enumValueIndex);
            }

            void DrawAudioPlayerSetting()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Audio Player".ToWhiteBold(), GUIStyleHelper.RichText);
                using (new EditorGUI.IndentLevelScope())
                {
                    using (new LabelWidthScope(EditorGUIUtility.labelWidth * 1.65f))
                    {
                        var logProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.LogAccessRecycledPlayerWarning));
                        Rect accessRecycledWarnRect = GetRectAndIterateLine(drawPosition);
                        logProp.boolValue = EditorGUI.Toggle(accessRecycledWarnRect, _logAccessRecycledWarningGUIContent, logProp.boolValue);

                        var poolSizeProp = _runtimeSettingSO.FindProperty(nameof(Data.RuntimeSetting.DefaultAudioPlayerPoolSize));
                        Rect maxPoolSizeRect = GetRectAndIterateLine(drawPosition);
                        float fieldWidth = maxPoolSizeRect.width - EditorGUIUtility.labelWidth;
                        maxPoolSizeRect.width -= fieldWidth - 50f;
                        poolSizeProp.intValue = EditorGUI.IntField(maxPoolSizeRect, _poolSizeCountGUIContent, poolSizeProp.intValue);
                    }
                }
            }

            void DrawAudioProjectSettings()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), ProjectSettings.ToWhiteBold(), GUIStyleHelper.RichText);

                using (new EditorGUI.IndentLevelScope())
                {
                    if (HasValidProjectSettingVoiceCount())
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        {
                            Rect voiceRect = GetRectAndIterateLine(drawPosition);
                            EditorGUI.LabelField(voiceRect, _audioVoicesGUIContent);
                            voiceRect.x += 150f;
                            voiceRect.width = 100f;
                            EditorGUI.IntField(voiceRect, _currProjectSettingVoiceCount);

                            Rect virtualTracksRect = GetRectAndIterateLine(drawPosition);
                            EditorGUI.LabelField(virtualTracksRect, _virtualTracksGUIContent);
                            virtualTracksRect.x += 150f;
                            virtualTracksRect.width = 100f;
                            EditorGUI.IntField(virtualTracksRect, _broVirtualTracksCount);
                        }
                        EditorGUI.EndDisabledGroup();
                    }

                    if (HasValidMixerTracksCount() && _currentMixerTracksCount < _currProjectSettingVoiceCount + _broVirtualTracksCount)
                    {
                        Rect warningBoxRect = GetRectAndIterateLine(drawPosition);
                        warningBoxRect.height *= 3;
                        warningBoxRect.width -= IndentInPixel;
                        string text = string.Format(_instruction.GetText(Instruction.TracksAndVoicesNotMatchWarning), MixerName.ToWhiteBold(), ProjectSettingsMenuItemPath.SetColor(GUIStyleHelper.LinkBlue));
                        RichTextHelpBox(warningBoxRect, text, MessageType.Warning);
                        if (GUI.Button(warningBoxRect, GUIContent.none, GUIStyle.none))
                        {
                            SettingsService.OpenProjectSettings(BroEditorUtility.AudioSettingPath);
                        }
                        EditorGUIUtility.AddCursorRect(warningBoxRect, MouseCursor.Link);

                        DrawEmptyLine(2); // For Help Box

                        Rect autoMatchBtnRect = GetRectAndIterateLine(drawPosition);
                        autoMatchBtnRect.height *= 2f;
                        autoMatchBtnRect.x += IndentInPixel *2;
                        autoMatchBtnRect.width -= IndentInPixel *2;
                        if (GUI.Button(autoMatchBtnRect, AutoMatchTracksButtonText)
                            && EditorUtility.DisplayDialog("Confirmation", _instruction.GetText(Instruction.AddTracksConfirmationDialog), "OK", "Cancel"))
                        {
                            AutoMatchAudioVoices();
                        }
                        DrawEmptyLine(2); // For Match Button
                    }
                }
            }
        }

        private void AutoMatchAudioVoices()
        {
            AudioMixerGroup mainTrack = AudioMixer.FindMatchingGroups(MainTrackName)?.Where(x => x.name.Length == MainTrackName.Length).FirstOrDefault();
            if (mainTrack == default || _currentMixerTracksCount == default)
            {
                Debug.LogError(LogTitle + "Can't get the Main track or other BroAudio track");
                return;
            }

            if(_duplicateTrackSource)
            {
                for(int i = _currentMixerTracksCount +1 ; i <= _currProjectSettingVoiceCount + _broVirtualTracksCount; i++)
                {
                    string trackName = $"{GenericTrackName}{i}";
                    BroAudioReflection.DuplicateBroAudioTrack(AudioMixer, mainTrack, _duplicateTrackSource, trackName);
                }
                // reset it to restart the checking
                ResetTracksAndAudioVoices();
            }
            else
            {
                Debug.LogError(LogTitle + "No valid track for duplicating");
            }
        }

        private void AddDominatorTrack()
        {
            AudioMixerGroup masterTrack = AudioMixer.FindMatchingGroups(MasterTrackName).FirstOrDefault();
            AudioMixerGroup[] dominatorTracks = AudioMixer.FindMatchingGroups(DominatorTrackName);
            if(masterTrack != null && dominatorTracks != null && dominatorTracks.Length > 0)
            {
                string trackName = $"{DominatorTrackName}{dominatorTracks.Length + 1}";
                BroAudioReflection.DuplicateBroAudioTrack(AudioMixer, masterTrack, dominatorTracks[dominatorTracks.Length - 1], trackName, ExposedParameterType.Volume);
            }
        }

        private bool HasValidProjectSettingVoiceCount()
        {
            if (_currProjectSettingVoiceCount < 0)
            {
                _currProjectSettingVoiceCount = 0; // if it's still 0 after the following search. then just skip for the rest of the time.
                _currProjectSettingVoiceCount = BroEditorUtility.GetProjectSettingRealAudioVoices();
            }
            return _currProjectSettingVoiceCount > 0;
        }

        private bool HasValidMixerTracksCount()
        {
            if(_currentMixerTracksCount < 0)
            {
                _currentMixerTracksCount = 0; // if it's still 0 after the following search. then just skip for the rest of the time.
                if (AudioMixer)
                {
                    var tracks = AudioMixer.FindMatchingGroups(GenericTrackName);
                    _currentMixerTracksCount = tracks != null ? tracks.Length : 0;
                    _duplicateTrackSource = tracks != null ? tracks.Last() : null;
                }
            }
            return _currentMixerTracksCount > 0;
        }

        private void DrawGUISetting(Rect drawPosition)
        {
            drawPosition.width -= 15f;
            var showVuProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowVUColorOnVolumeSlider));
            var showMasterProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowMasterVolumeOnClipListHeader));
            var showAudioTypeProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowAudioTypeOnSoundID));
            var showPlayButtonWhenCollapsed = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowPlayButtonWhenEntityCollapsed));
            var openLastEditedAssetProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.OpenLastEditAudioAsset));

            showVuProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), VUColorToggleLabel, showVuProp.boolValue);
            DemonstrateSlider();

            showMasterProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowMasterVolumeLabel, showMasterProp.boolValue);
            showPlayButtonWhenCollapsed.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowPlayButtonWhenCollapsed, showPlayButtonWhenCollapsed.boolValue);
            openLastEditedAssetProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), OpenLastEditedAssetLabel, openLastEditedAssetProp.boolValue);
            showAudioTypeProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowAudioTypeToggleLabel, showAudioTypeProp.boolValue);
            if (showAudioTypeProp.boolValue)
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AudioTypeColorLabel.ToWhiteBold(), GUIStyleHelper.RichText);
                using (new EditorGUI.IndentLevelScope())
                {
                    Rect colorRect = GetRectAndIterateLine(drawPosition);
                    DrawTwoColumnAudioType(colorRect, DrawAudioTypeLabelColorField);
                }
            }

            EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AudioTypeDrawedProperties.ToWhiteBold(), GUIStyleHelper.RichText);
            using (new EditorGUI.IndentLevelScope())
            {
                DrawTwoColumnAudioType(GetRectAndIterateLine(drawPosition), DrawAudioTypeDrawedPropertiesField);
            }

            EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), SpectrumBandColors.ToWhiteBold(), GUIStyleHelper.RichText);
            Rect colorListRect = GetRectAndIterateLine(drawPosition);
            colorListRect.x += IndentInPixel;
            colorListRect.xMax -= IndentInPixel;
            colorListRect.height = _spectrumColorsList.GetHeight();
            _spectrumColorsList.DoList(colorListRect);
            DrawEmptyLine(_spectrumColorsList.count + 1);
            DrawEmptyLine(1);

            void DemonstrateSlider()
            {
                Rect sliderRect = GetRectAndIterateLine(drawPosition);
                sliderRect.width = drawPosition.width * 0.5f;
                sliderRect.x += Gap;
                if (showVuProp.boolValue)
                {
                    Rect vuRect = new Rect(sliderRect);
                    vuRect.height *= 0.5f;
                    EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent(HorizontalVUMeter).image);
                    EditorGUI.DrawRect(vuRect, VUMaskColor);
                }
                _demoSliderValue = GUI.HorizontalSlider(sliderRect, _demoSliderValue, 0f, 1.25f);
            }

            void DrawTwoColumnAudioType(Rect colorRect, Action<Rect, BroAudioType> onDraw)
            {
                SplitRectHorizontal(colorRect, 0.5f, 0f, out Rect leftColorRect, out Rect rightColorRect);
                int count = 0;
                ForeachConcreteAudioType((audioType) =>
                {
                    onDraw?.Invoke(count % 2 == 0 ? leftColorRect : rightColorRect, audioType);
                    count++;
                    if (count % 2 == 1)
                    {
                        leftColorRect.y += SingleLineSpace;
                        rightColorRect.y += SingleLineSpace;
                        DrawEmptyLine(1);
                    }
                });
            }

            void DrawAudioTypeLabelColorField(Rect fieldRect, BroAudioType audioType)
            {
                if(EditorSetting.TryGetAudioTypeSetting(audioType,out var setting))
                {
                    EditorGUI.BeginChangeCheck();
                    setting.Color = EditorGUI.ColorField(fieldRect, audioType.ToString(), setting.Color);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorSetting.WriteAudioTypeSetting(audioType, setting);
                    }
                }
            }

            void DrawAudioTypeDrawedPropertiesField(Rect fieldRect, BroAudioType audioType)
            {
                if (EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
                {
                    EditorGUI.BeginChangeCheck();
                    setting.DrawedProperty = (DrawedProperty)EditorGUI.EnumFlagsField(fieldRect, audioType.ToString(), setting.DrawedProperty);
                    if(EditorGUI.EndChangeCheck())
                    {
                        EditorSetting.WriteAudioTypeSetting(audioType, setting);
                    }                  
                }
            }
        }

        private void DrawMiscellaneousSetting(Rect drawPosition)
        {
            DrawAssetOutputPath(drawPosition);
            DrawEmptyLine(1);

#if PACKAGE_ADDRESSABLES
            DrawAddressableNeverAskOptions(drawPosition);
            DrawEmptyLine(1);
#endif

            if (Button(_dominatorTrackGUIContent))
            {
                AddDominatorTrack();
            }
            DrawEmptyLine(1);


            if (Button(_regenerateUserDataGUIContent))
            {
                BroUserDataGenerator.CheckAndGenerateUserData();
            }
            DrawEmptyLine(1);

            if (Button(new GUIContent(ResetSettingButtonText)))
            {
                Undo.RecordObjects(new UnityEngine.Object[] { RuntimeSetting, EditorSetting }, "Reset BroAudio Preference Settings");
                RuntimeSetting.ResetToFactorySettings();
                EditorSetting.ResetToFactorySettings();
            }
            DrawEmptyLine(1);

            bool Button(GUIContent label)
            {
                Rect buttonRect = GetRectAndIterateLine(drawPosition).GetHorizontalCenterRect(400f, SingleLineSpace * 1.5f);
                return GUI.Button(buttonRect, label);
            }
        }

#if PACKAGE_ADDRESSABLES
        private void DrawAddressableNeverAskOptions(Rect drawPosition)
        {
            drawPosition.xMax -= Gap;
            EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), _addressableConversionGUIContent, GUIStyleHelper.RichText);
            using (new EditorGUI.IndentLevelScope())
            {
                var directDecisionProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.DirectReferenceDecision));
                var addressableDecisionProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.AddressableDecision));
                DrawOption(GetRectAndIterateLine(drawPosition), _directToAddressableGUIContent, directDecisionProp);
                DrawOption(GetRectAndIterateLine(drawPosition), _addressableToDirectGUIContent, addressableDecisionProp);
            }

            void DrawOption(Rect rect, GUIContent label, SerializedProperty property)
            {
                SplitRectHorizontal(rect, 0.4f, 10f, out Rect labelRect, out Rect popupRect);
                EditorGUI.LabelField(labelRect, label);
                property.enumValueIndex = (int)(EditorSetting.ReferenceConversionDecision)EditorGUI.EnumPopup(popupRect, (EditorSetting.ReferenceConversionDecision)property.enumValueIndex);
            }
        } 
#endif

        private void DrawAssetOutputPath(Rect drawPosition)
        {
            EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AssetOutputPathLabel, GUIStyleHelper.MiddleCenterRichText);
            if(!_hasOutputAssetPath)
            {
                RichTextHelpBox(GetRectAndIterateLine(drawPosition).GetHorizontalCenterRect(drawPosition.width * 0.7f, SingleLineSpace *2), AssetOutputPathMissing, MessageType.Error);
                DrawEmptyLine(1);
            }

            Rect rect = GetRectAndIterateLine(drawPosition).GetHorizontalCenterRect(drawPosition.width * 0.7f, SingleLineSpace);
            BroEditorUtility.DrawAssetOutputPath(rect, _instruction, () => _hasOutputAssetPath = true);
        }
    }
}