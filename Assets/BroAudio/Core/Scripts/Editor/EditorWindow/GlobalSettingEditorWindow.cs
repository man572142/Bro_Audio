using Ami.Extension;
using Ami.Extension.Reflection;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.IconConstant;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.BroAudio.Tools.BroLog;
using static Ami.BroAudio.Tools.BroName;
using static Ami.BroAudio.Utility;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor.Setting
{
    public class GlobalSettingEditorWindow : MiEditorWindow
	{
		public enum OpenMessage
		{
			None,
			Welcome,
			RuntimeSettingFileMissing,
			EditorSettingFileMissing,
		}

		public enum Tab { Audio, GUI, Miscellaneous,}

		public const string SettingFileName = "BroAudioGlobalSetting";
		public const float Gap = 50f;
		
		public const string ResetSettingButtonText = "Reset To Factory Settings";
		public const string AutoMatchTracksButtonText = "Auto-adding tracks to match audio voices.";
		public const string AssetOutputPathLabel = "Asset Output Path";
		public const string VUColorToggleLabel = "Show VU color on volume slider";
		public const string ShowAudioTypeToggleLabel = "Show audioType on SoundID";
		public const string AudioTypeColorLabel = "Audio Type Color";
		public const string AudioTypeDrawedProperties = "Displayed Properties";
		public const string AddDominatorTrackLabel = "Add Dominator Track";
		public const string ProjectSettingsMenuItemPath = "Edit/" + ProjectSettings;
		public const string ProjectSettings = "Project Settings";
		public const string AutoFixDocUrl = "https://man572142s-organization.gitbook.io/broaudio/core-features/audio-mixer";

		private readonly float[] _tabLabelRatios = new float[] { 0.33f,0.33f,0.34f};

        private GUIContent _combFilteringGUIContent, _pitchGUIContent, _audioVoicesGUIContent, _virtualTracksGUIContent, _filterSlopeGUIContent, _acceptAudioMixerGUIContent
			,_playMusicAsBgmGUIContent;

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
		private bool _hasGUIContentInit = false;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public OpenMessage Message { get; private set; } = OpenMessage.None;
		public EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
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

		[MenuItem(GlobalSettingMenuPath,false,GlobalSettingMenuIndex)]
		public static GlobalSettingEditorWindow ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(GlobalSettingEditorWindow));
			window.minSize = new Vector2(640f, 480f);
			window.titleContent = new GUIContent(MenuItem_Setting, EditorGUIUtility.IconContent("EditorSettings Icon").image);
			window.Show();
			return window as GlobalSettingEditorWindow;
		}

		public static void ShowWindowWithMessage(OpenMessage message)
		{
			GlobalSettingEditorWindow window = ShowWindow();
			if(window != null)
			{
				window.SetOpenMessage(message);
			}
		}

		public void SetOpenMessage(OpenMessage message)
		{
			Message = message;
		}

		private void InitGUIContents()
		{
			_combFilteringGUIContent = new GUIContent("Time To Prevent Comb Filtering", _instruction.GetText(Instruction.CombFilteringTooltip));
			_pitchGUIContent = new GUIContent("Pitch Shift Using", _instruction.GetText(Instruction.PitchShiftingToolTip));
			_audioVoicesGUIContent = new GUIContent("Max Real Voices", _instruction.GetText(Instruction.AudioVoicesToolTip));
			_virtualTracksGUIContent = new GUIContent("Bro Virtual Tracks", _instruction.GetText(Instruction.BroVirtualToolTip));
			_filterSlopeGUIContent = new GUIContent("Audio Filter Slope", _instruction.GetText(Instruction.AudioFilterSlope));
			_acceptAudioMixerGUIContent = new GUIContent("Accept BroAudioMixer Modification", _instruction.GetText(Instruction.AcceptAudioMixerModification));
			_playMusicAsBgmGUIContent = new GUIContent("Always Play Music As BGM", _instruction.GetText(Instruction.AlwaysPlayMusicAsBGM));
		}

		private void OnDisable()
		{
			ResetTracksAndAudioVoices();
		}

		private void OnLostFocus()
		{
			EditorUtility.SetDirty(RuntimeSetting);
			EditorUtility.SetDirty(EditorSetting);
		}

		private void ResetTracksAndAudioVoices()
		{
			_currProjectSettingVoiceCount = -1;
			_currentMixerTracksCount = -1;
		}

		private void InitTabsLabel()
		{
            _tabLabels = _tabLabels ?? new GUIContent[3];

			_tabLabels[(int)Tab.Audio] = EditorGUIUtility.IconContent(AudioSettingTab);
			_tabLabels[(int)Tab.GUI] = EditorGUIUtility.IconContent(GUISettingTab);
			_tabLabels[(int)Tab.Miscellaneous] = EditorGUIUtility.IconContent(CogIcon);
		}

		protected override void OnGUI()
        {
            base.OnGUI();

			if(!_hasGUIContentInit)
			{
				InitTabsLabel();
				InitGUIContents();
				_hasGUIContentInit = true;
			}

            Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

			if (Message != OpenMessage.None)
            {
                DrawEmptyLine(1);
                switch (Message)
                {
                    case OpenMessage.RuntimeSettingFileMissing:
                    case OpenMessage.EditorSettingFileMissing:
                        Rect errorRect = GetRectAndIterateLine(drawPosition);
                        errorRect.height *= 2;
                        Instruction instructionEnum = GetInstructionEnum(Message);
                        EditorGUI.HelpBox(errorRect, _instruction.GetText(instructionEnum), MessageType.Error);
                        DrawEmptyLine(1);
                        break;
                }
            }

            DrawEmptyLine(1);

            drawPosition.x += Gap;
            drawPosition.width -= Gap * 2;

            Rect tabWindowRect = GetRectAndIterateLine(drawPosition);
            tabWindowRect.yMax = drawPosition.yMax;
            _tabPreAllocRects = _tabPreAllocRects ?? new Rect[_tabLabelRatios.Length];
            _currSelectedTab = (Tab)DrawTabsView(tabWindowRect, (int)_currSelectedTab, TabLabelHeight, _tabLabels, _tabLabelRatios, _tabPreAllocRects);

            EditorGUI.indentLevel++;
            Rect tabPageScrollRect = new Rect(tabWindowRect.x, tabWindowRect.y + TabLabelHeight, tabWindowRect.width, tabWindowRect.height - TabLabelHeight);
            _scrollPos = BeginScrollView(tabPageScrollRect, _scrollPos);
            DrawEmptyLine(1);
            if (RuntimeSetting != null)
            {
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
            }
            EndScrollView();
            EditorGUI.indentLevel--;
        }

        private Instruction GetInstructionEnum(OpenMessage message)
		{
			switch (message)
			{
				case OpenMessage.RuntimeSettingFileMissing:
					return Instruction.RuntimeSettingFileMissing;
				case OpenMessage.EditorSettingFileMissing:
					return Instruction.EditorSettingFileMissing;
			}
			return default;
		}

        private void DrawAudioSetting(Rect drawPosition)
        {
            drawPosition.width -= Gap;
			DrawAudioFilterSlope();
			DrawEmptyLine(1);
			DrawBGMSetting();
			DrawCombFilteringSetting();
            // To make a room for other functions to use exposed parameters, we only use AudioSource.pitch for now
            //DrawPitchSetting();
            DrawEmptyLine(1);
            DrawDefaultEasing();
            DrawSeamlessLoopEasing();
            DrawEmptyLine(1);
            DrawAudioProjectSettings();

			void DrawBGMSetting()
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "BGM".ToWhiteBold(), GUIStyleHelper.RichText);
				EditorGUI.indentLevel++;
				Rect toggleRect = GetRectAndIterateLine(drawPosition);
				using (new LabelWidthScope(EditorGUIUtility.labelWidth * 1.3f))
				{
					RuntimeSetting.AlwaysPlayMusicAsBGM = EditorGUI.Toggle(toggleRect, _playMusicAsBgmGUIContent, RuntimeSetting.AlwaysPlayMusicAsBGM);

					if(RuntimeSetting.AlwaysPlayMusicAsBGM)
					{
						RuntimeSetting.DefaultBGMTransition =
						(Transition)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Default Transition", RuntimeSetting.DefaultBGMTransition);
					}
				}
				EditorGUI.indentLevel--;
			}

			void DrawCombFilteringSetting()
            {
				DrawEmptyLine(1);
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Comb Filtering".ToWhiteBold(), GUIStyleHelper.RichText);
				EditorGUI.indentLevel++;
				Rect combRect = GetRectAndIterateLine(drawPosition);
                EditorGUI.LabelField(combRect, _combFilteringGUIContent);

                Rect fieldRect = new Rect(combRect) { width = 80f, x = combRect.x + EditorGUIUtility.labelWidth + 50f };
                RuntimeSetting.CombFilteringPreventionInSeconds = EditorGUI.FloatField(fieldRect, RuntimeSetting.CombFilteringPreventionInSeconds);

                Rect defaultButtonRect = new Rect(fieldRect) { x = fieldRect.xMax + 10f, width = 60f };
                float defaultValue = Data.RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds;
                EditorGUI.BeginDisabledGroup(RuntimeSetting.CombFilteringPreventionInSeconds == defaultValue);
                if (GUI.Button(defaultButtonRect, "Default"))
                {
                    RuntimeSetting.CombFilteringPreventionInSeconds = defaultValue;
                }
                EditorGUI.EndDisabledGroup();

				using (new LabelWidthScope(EditorGUIUtility.labelWidth * 1.2f))
				{
					RuntimeSetting.LogCombFilteringWarning = EditorGUI.Toggle(GetRectAndIterateLine(drawPosition), "Log Warning If Occurs", RuntimeSetting.LogCombFilteringWarning);
				}
				EditorGUI.indentLevel--;
			}

            //void DrawPitchSetting()
            //{
            //	Rect pitchRect = GetRectAndIterateLine(drawPosition);
            //	bool isWebGL = EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
            //	if(isWebGL)
            //	{
            //		EditorGUI.BeginDisabledGroup(isWebGL);
            //		EditorGUI.EnumPopup(pitchRect, _pitchGUIContent, PitchShiftingSetting.AudioSource);
            //		EditorGUI.EndDisabledGroup();
            //	}
            //	else
            //	{
            //		RuntimeSetting.PitchSetting = (PitchShiftingSetting)EditorGUI.EnumPopup(pitchRect, _pitchGUIContent, RuntimeSetting.PitchSetting);
            //	}
            //}

            void DrawDefaultEasing()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Default Easing".ToWhiteBold(), GUIStyleHelper.RichText);
                EditorGUI.indentLevel++;
                RuntimeSetting.DefaultFadeInEase =
                    (Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", RuntimeSetting.DefaultFadeInEase);
                RuntimeSetting.DefaultFadeOutEase =
                    (Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", RuntimeSetting.DefaultFadeOutEase);
                EditorGUI.indentLevel--;
            }

            void DrawSeamlessLoopEasing()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Seamless Loop Easing".ToWhiteBold(), GUIStyleHelper.RichText);
                EditorGUI.indentLevel++;
                RuntimeSetting.SeamlessFadeInEase =
                    (Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", RuntimeSetting.SeamlessFadeInEase);
                RuntimeSetting.SeamlessFadeOutEase =
                    (Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", RuntimeSetting.SeamlessFadeOutEase);
                EditorGUI.indentLevel--;
            }

            void DrawAudioFilterSlope()
            {
                RuntimeSetting.AudioFilterSlope = (FilterSlope)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), _filterSlopeGUIContent, RuntimeSetting.AudioFilterSlope);
            }

            void DrawAudioProjectSettings()
            {
                EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), ProjectSettings.ToWhiteBold(), GUIStyleHelper.RichText);

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
                    Color linkBlue = GUIStyleHelper.LinkLabelStyle.normal.textColor;
                    string text = string.Format(_instruction.GetText(Instruction.TracksAndVoicesNotMatchWarning), MixerName.ToWhiteBold(), ProjectSettingsMenuItemPath.SetColor(linkBlue));
                    RichTextHelpBox(warningBoxRect, text, MessageType.Warning);
                    if (GUI.Button(warningBoxRect, GUIContent.none, GUIStyle.none))
                    {
                        SettingsService.OpenProjectSettings(AudioSettingPath);
                    }
                    EditorGUIUtility.AddCursorRect(warningBoxRect, MouseCursor.Link);

                    DrawEmptyLine(2); // For Help Box

                    Rect autoMatchBtnRect = GetRectAndIterateLine(drawPosition);
                    autoMatchBtnRect.height *= 2f;
                    if (GUI.Button(autoMatchBtnRect, AutoMatchTracksButtonText)
                        && EditorUtility.DisplayDialog("Confirmation", _instruction.GetText(Instruction.AddTracksConfirmationDialog), "OK", "Cancel"))
                    {
                        AutoMatchAudioVoices();
                    }
                    DrawEmptyLine(2); // For Match Button
                }
            }
        }

		private void AutoMatchAudioVoices()
		{
			AudioMixerGroup mainTrack = AudioMixer.FindMatchingGroups(MainTrackName)?.Where(x => x.name.Length == MainTrackName.Length).FirstOrDefault();
			if (mainTrack == default || _currentMixerTracksCount == default)
			{
				LogError("Can't get the Main track or other BroAudio track");
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
				LogError("No valid track for duplicating");
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
				_currProjectSettingVoiceCount = GetProjectSettingRealAudioVoices();
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
			EditorSetting.ShowVUColorOnVolumeSlider = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), VUColorToggleLabel, EditorSetting.ShowVUColorOnVolumeSlider);
			DemonstrateSlider();

			EditorSetting.ShowAudioTypeOnSoundID = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowAudioTypeToggleLabel, EditorSetting.ShowAudioTypeOnSoundID);

			if (EditorSetting.ShowAudioTypeOnSoundID)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AudioTypeColorLabel.ToWhiteBold(), GUIStyleHelper.RichText);
				EditorGUI.indentLevel++;
				Rect colorRect = GetRectAndIterateLine(drawPosition);
				colorRect.xMax -= 20f;

				DrawTwoColumnAudioType(colorRect, SetAudioTypeLabelColor);
				EditorGUI.indentLevel--;
			}

			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AudioTypeDrawedProperties.ToWhiteBold(), GUIStyleHelper.RichText);
			EditorGUI.indentLevel++;
			Rect drawedPropRect = GetRectAndIterateLine(drawPosition);

			DrawTwoColumnAudioType(drawedPropRect, SetAudioTypeDrawedProperties);
			EditorGUI.indentLevel--;

			void DemonstrateSlider()
			{
				Rect sliderRect = GetRectAndIterateLine(drawPosition);
				sliderRect.width = drawPosition.width * 0.5f;
				sliderRect.x += Gap;
				if (EditorSetting.ShowVUColorOnVolumeSlider)
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

			void SetAudioTypeLabelColor(Rect fieldRect, BroAudioType audioType)
			{
				if(EditorSetting.TryGetAudioTypeSetting(audioType,out var setting))
				{
					setting.Color = EditorGUI.ColorField(fieldRect, audioType.ToString(), setting.Color);
					EditorSetting.WriteAudioTypeSetting(audioType, setting);
				}
			}

			void SetAudioTypeDrawedProperties(Rect fieldRect, BroAudioType audioType)
			{
				if (EditorSetting.TryGetAudioTypeSetting(audioType, out var setting))
				{
					setting.DrawedProperty = (DrawedProperty)EditorGUI.EnumFlagsField(fieldRect, audioType.ToString(), setting.DrawedProperty);
					EditorSetting.WriteAudioTypeSetting(audioType, setting);
				}
			}
		}

		private void DrawMiscellaneousSetting(Rect drawPosition)
		{
			DrawAutoFixOption(drawPosition);
			DrawEmptyLine(1);
			DrawAssetOutputPath(drawPosition);
			DrawEmptyLine(1);

			if(Button(AddDominatorTrackLabel))
			{
				AddDominatorTrack();
			}
            DrawEmptyLine(1);

            if (Button(ResetSettingButtonText))
			{
                RuntimeSetting.ResetToFactorySettings();
                EditorSetting.ResetToFactorySettings();
            }
            DrawEmptyLine(1);

			bool Button(string label)
			{
                Rect buttonRect = GetRectAndIterateLine(drawPosition).GetHorizontalCenterRect(400f, SingleLineSpace * 1.5f);
				return GUI.Button(buttonRect, label);
            }
        }

		private void DrawAutoFixOption(Rect drawPosition)
		{
#if UNITY_2021 && UNITY_EDITOR
			EditorSetting.AcceptAudioMixerModification = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), _acceptAudioMixerGUIContent, EditorSetting.AcceptAudioMixerModification);
			Rect infoBoxRect = GetRectAndIterateLine(drawPosition);
			infoBoxRect.height *= 3;
			Color linkBlue = GUIStyleHelper.LinkLabelStyle.normal.textColor;
			string text = string.Format(_instruction.GetText(Instruction.AcceptAudioMixerModification), "Documentation".SetColor(linkBlue));
			RichTextHelpBox(infoBoxRect, text, MessageType.Info);
			if (GUI.Button(infoBoxRect, GUIContent.none, GUIStyle.none))
			{
				Application.OpenURL(AutoFixDocUrl);
			}
			EditorGUIUtility.AddCursorRect(infoBoxRect, MouseCursor.Link);
			DrawEmptyLine(1);
#endif
		}

		private void DrawAssetOutputPath(Rect drawPosition)
		{
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AssetOutputPathLabel, GUIStyleHelper.MiddleCenterRichText);

			GUIStyle style = new GUIStyle(EditorStyles.objectField);
			style.alignment = TextAnchor.MiddleCenter;
			Rect rect = GetRectAndIterateLine(drawPosition).GetHorizontalCenterRect(drawPosition.width * 0.7f, SingleLineSpace);
			if (GUI.Button(rect, new GUIContent(AssetOutputPath), style))
			{
				string openPath = AssetOutputPath;
				if (!Directory.Exists(GetFullPath(openPath)))
				{
					openPath = Application.dataPath;
				}
				string newPath = EditorUtility.OpenFolderPanel(_instruction.GetText(Instruction.AssetOutputPathPanelTtile),openPath , "");
				if (!string.IsNullOrEmpty(newPath) && IsInProjectFolder(newPath))
				{
					newPath = newPath.Remove(0, UnityProjectRootPath.Length + 1);
					AssetOutputPath = newPath;
                    WriteAssetOutputPathToCoreData(newPath);
                }
			}
			Rect browserIconRect = rect;
			browserIconRect.width = EditorGUIUtility.singleLineHeight;
			browserIconRect.height = EditorGUIUtility.singleLineHeight;
			browserIconRect.x = rect.xMax - EditorGUIUtility.singleLineHeight;
#if UNITY_2020_1_OR_NEWER
			GUI.DrawTexture(browserIconRect, EditorGUIUtility.IconContent(AssetOutputBrowser).image);
#endif
			EditorGUI.DrawRect(browserIconRect, BroAudioGUISetting.ShadowMaskColor);
		}
	}
}