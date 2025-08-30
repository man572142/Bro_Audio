using Ami.Extension;
using Ami.Extension.Reflection;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using static Ami.BroAudio.Editor.IconConstant;
using static Ami.BroAudio.Editor.Section;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Utility;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor.Setting
{
    public class PreferencesEditorWindow : MiEditorWindow
    {
        public enum Tab { Audio, GUI, Miscellaneous,}

        private const float Gap = 50f;
        public const string ResetSettingButtonText = "Reset To Factory Settings";
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

        private GUIContent _pitchGUIContent, _audioVoicesGUIContent, _virtualTracksGUIContent, _dominatorTrackGUIContent, _regenerateUserDataGUIContent;

#if PACKAGE_ADDRESSABLES
        private GUIContent _addressableConversionGUIContent; 
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
        private PreferencesDrawer _preferenceDrawer;

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
            _preferenceDrawer = new PreferencesDrawer(_runtimeSettingSO, _editorSettingSO, _instruction);

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
            _dominatorTrackGUIContent = new GUIContent("Add Dominator Track", _instruction.GetText(Instruction.AddDominatorTrack));
            _regenerateUserDataGUIContent = new GUIContent("Regenerate User Data", _instruction.GetText(Instruction.RegenerateUserData));
            
#if PACKAGE_ADDRESSABLES
            string aaTooltip = _instruction.GetText(Instruction.LibraryManager_AddressableConversionTooltip);
            _addressableConversionGUIContent = new GUIContent("Addressable References Conversion".ToWhiteBold(), aaTooltip);
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

        private void DrawAudioSetting(Rect drawPos)
        {
            drawPos.width -= Gap;
            _preferenceDrawer.DrawGlobalPlaybackGroup(GetRectAndIterateLine(drawPos));
            _preferenceDrawer.DrawAudioFilterSlope(GetRectAndIterateLine(drawPos));
            _preferenceDrawer.DrawUpdateMode(GetRectAndIterateLine(drawPos));
            DrawEmptyLine(1);
            using (NewSection("BGM", GetRectAndIterateLine(drawPos), 1.3f)) 
            {
                _preferenceDrawer.DrawBGMSetting(
                    GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos).SetWidth(250f));
            }
            DrawEmptyLine(1);
            using (NewSection("Chained Play Mode", GetRectAndIterateLine(drawPos), 1.3f))
            {
                _preferenceDrawer.DrawChainedPlayMode(
                    GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos));
            }

            // To make room for other functions to use exposed parameters, we only use AudioSource.pitch for now
            //DrawPitchSetting();
            DrawEmptyLine(1);
            using (NewSection("Default Easing", GetRectAndIterateLine(drawPos)))
            {
                _preferenceDrawer.DrawDefaultEasing(GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos));
            }
            DrawEmptyLine(1);
            using (NewSection("Seamless Loop Easing", GetRectAndIterateLine(drawPos)))
            {
                _preferenceDrawer.DrawSeamlessLoopEasing(GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos));
            }
            DrawEmptyLine(1);
            using (NewSection("Audio Player", GetRectAndIterateLine(drawPos), 1.65f))
            {
                _preferenceDrawer.DrawAudioPlayerSetting(
                    GetRectAndIterateLine(drawPos), GetRectAndIterateLine(drawPos));
            }
            DrawEmptyLine(1);
            using (NewSection(ProjectSettings, GetRectAndIterateLine(drawPos)))
            {
                DrawAudioProjectSettings(drawPos);
            }
        }
        
        private void DrawAudioProjectSettings(Rect drawPos)
        {
            if (HasValidProjectSettingVoiceCount())
            {
                EditorGUI.BeginDisabledGroup(true);
                {
                    Rect voiceRect = GetRectAndIterateLine(drawPos);
                    EditorGUI.LabelField(voiceRect, _audioVoicesGUIContent);
                    voiceRect.x += 150f;
                    voiceRect.width = 100f;
                    EditorGUI.IntField(voiceRect, _currProjectSettingVoiceCount);

                    Rect virtualTracksRect = GetRectAndIterateLine(drawPos);
                    EditorGUI.LabelField(virtualTracksRect, _virtualTracksGUIContent);
                    virtualTracksRect.x += 150f;
                    virtualTracksRect.width = 100f;
                    EditorGUI.IntField(virtualTracksRect, _broVirtualTracksCount);
                }
                EditorGUI.EndDisabledGroup();
            }

            if (HasValidMixerTracksCount() && _currentMixerTracksCount < _currProjectSettingVoiceCount + _broVirtualTracksCount)
            {
                Rect warningBoxRect = GetRectAndIterateLine(drawPos);
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

                Rect autoMatchBtnRect = GetRectAndIterateLine(drawPos);
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

        private void DrawGUISetting(Rect drawPos)
        {
            drawPos.width -= 15f;
            var showVuProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowVUColorOnVolumeSlider));
            var showMasterProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowMasterVolumeOnClipListHeader));
            var showAudioTypeProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowAudioTypeOnSoundID));
            var showPlayButtonWhenCollapsed = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.ShowPlayButtonWhenEntityCollapsed));
            var openLastEditedAssetProp = _editorSettingSO.FindProperty(nameof(Editor.EditorSetting.OpenLastEditAudioAsset));

            showVuProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPos), VUColorToggleLabel, showVuProp.boolValue);
            Rect sliderRect = GetRectAndIterateLine(drawPos).SetWidth(drawPos.width * 0.5f);
            sliderRect.x += Gap;
            PreferencesDrawer.DemonstrateSlider(sliderRect, showVuProp.boolValue, ref _demoSliderValue);

            showMasterProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPos), ShowMasterVolumeLabel, showMasterProp.boolValue);
            showPlayButtonWhenCollapsed.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPos), ShowPlayButtonWhenCollapsed, showPlayButtonWhenCollapsed.boolValue);
            openLastEditedAssetProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPos), OpenLastEditedAssetLabel, openLastEditedAssetProp.boolValue);
            showAudioTypeProp.boolValue = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPos), ShowAudioTypeToggleLabel, showAudioTypeProp.boolValue);
            _preferenceDrawer.DemonstrateSoundIDField(GetRectAndIterateLine(drawPos));
            DrawEmptyLine(1);
            if (showAudioTypeProp.boolValue)
            {
                using (NewSection(AudioTypeColorLabel, GetRectAndIterateLine(drawPos)))
                {
                    _preferenceDrawer.DrawAudioTypeColorSettings(GetRectAndIterateLine(drawPos), SingleLineSpace, DrawEmptyLine);
                }
            }

            using (NewSection(AudioTypeDrawedProperties, GetRectAndIterateLine(drawPos)))
            {
                _preferenceDrawer.DrawAudioTypeDrawedProperties(GetRectAndIterateLine(drawPos), SingleLineSpace, DrawEmptyLine);
            }

            EditorGUI.LabelField(GetRectAndIterateLine(drawPos), SpectrumBandColors.ToWhiteBold(), GUIStyleHelper.RichText);
            Rect colorListRect = GetRectAndIterateLine(drawPos);
            colorListRect.x += IndentInPixel;
            colorListRect.xMax -= IndentInPixel;
            colorListRect.height = _spectrumColorsList.GetHeight();
            _spectrumColorsList.DoList(colorListRect);
            DrawEmptyLine(_spectrumColorsList.count + 1);
            DrawEmptyLine(1);
        }

        private void DrawMiscellaneousSetting(Rect drawPosition)
        {
            DrawAssetOutputPath(drawPosition);
            DrawEmptyLine(1);

#if PACKAGE_ADDRESSABLES
            drawPosition.xMax -= Gap;
            using (NewSection(_addressableConversionGUIContent, GetRectAndIterateLine(drawPosition)))
            {
                _preferenceDrawer.DrawAddressableNeverAskOptions(
                    GetRectAndIterateLine(drawPosition), GetRectAndIterateLine(drawPosition));
            }
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