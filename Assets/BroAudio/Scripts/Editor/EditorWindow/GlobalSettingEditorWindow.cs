using Ami.BroAudio.Tools;
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

		public enum Tab { Audio, GUI, Info,	}

		public const string SettingFileName = "BroAudioGlobalSetting";
		public const float Gap = 50f;
		public const float CreditsPrefixWidth = 70f;
		public const int CreditsFieldCount = 5;
		
		public const string CombFilteringLabel = "Time to prevent Comb Filtering";
		public const string PitchShiftingLabel = "Pitch Shift Using";
		public const string ResetSettingButtonText = "Reset To Factory Settings";
		public const string AutoMatchTracksButtonText = "Auto-adding tracks to match audio voices.";
		public const string AssetOutputPathLabel = "Asset Output Path";
		public const string VUColorToggleLabel = "Show VU color on volume slider";
		public const string ShowAudioTypeToggleLabel = "Show audioType on AudioID";
		public const string AudioTypeColorLabel = "Audio Type Color";
		public const string AudioTypeDrawedProperties = "Displayed Properties";
		public const string GitURL = "https://github.com/man572142/Bro_Audio";
		public const string ProjectSettingsMenuItemPath = "Edit/" + ProjectSettings;
		public const string ProjectSettings = "Project Settings";
		public const string RealVoicesParameterName = "Max Real Voices";
		public const string BroVirtualTracks = "Bro Virtual Tracks";

		private readonly float[] _tabLabelRatios = new float[] { 0.33f,0.33f,0.34f};

		private GUIContent[] _tabLabels = null;
		private Tab _currSelectedTab = Tab.Audio;
		private int _currProjectSettingVoiceCount = -1;
		private int _currentMixerTracksCount = -1;
		private int _broVirtualTracksCount = BroAdvice.VirtualTrackCount;
		private GUIContent _combFilteringGUIContent , _pitchGUIContent, _audioVoicesGUIContent, _virtualTracksGUIContent;
		private BroInstructionHelper _instruction = new BroInstructionHelper();
		private AudioMixerGroup _duplicateTrackSource = null;
		private AudioMixer _mixer = null;
		private Vector2 _scrollPos = default;
		private float _demoSliderValue = 1f;
		private Rect[] _tabPreAllocRects = null;
		private UnityEngine.Object[] _creditsObjects = null;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public OpenMessage Message { get; private set; } = OpenMessage.None;
		public EditorSetting EditorSetting => BroEditorUtility.EditorSetting;
		private float TabLabelHeight => EditorGUIUtility.singleLineHeight * 2f;
		private Vector2 LogoSize => new Vector2(100f, 100f);

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

		private void OnEnable()
		{
			InitTabsLabel();
			InitGUIContents();
		}

		private void InitGUIContents()
		{
			_combFilteringGUIContent = new GUIContent(CombFilteringLabel, _instruction.GetText(Instruction.CombFilteringTooltip));
			_pitchGUIContent = new GUIContent(PitchShiftingLabel, _instruction.GetText(Instruction.PitchShiftingToolTip));
			_audioVoicesGUIContent = new GUIContent(RealVoicesParameterName, _instruction.GetText(Instruction.AudioVoicesToolTip));
			_virtualTracksGUIContent = new GUIContent(BroVirtualTracks, _instruction.GetText(Instruction.BroVirtualToolTip));
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
			_tabLabels[(int)Tab.Info] = EditorGUIUtility.IconContent(InfoTab);
		}

		protected override void OnGUI()
        {
            base.OnGUI();
            Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

			DrawEmptyLine(1);

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

            DrawAssetOutputPath(drawPosition);
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
            DrawEmptyLine(2);
            if (RuntimeSetting != null)
            {
                switch (_currSelectedTab)
                {
                    case Tab.Audio:
                        DrawAudioSetting(drawPosition);
                        break;
                    case Tab.GUI:
                        DrawGUISetting(drawPosition);
                        break;
                    case Tab.Info:
                        DrawInfo(drawPosition);
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
			DrawCombFilteringSetting();
            // To make a room for other functionality to use exposed parameters, we only use AudioSource.pitch for now
            //DrawPitchSetting();
            DrawEmptyLine(1);

			DrawDefaultEasing();
			DrawSeamlessLoopEasing();
			DrawEmptyLine(1);
			DrawAudioProjectSettings();

			void DrawCombFilteringSetting()
			{
				Rect combRect = GetRectAndIterateLine(drawPosition);
				EditorGUI.LabelField(combRect, _combFilteringGUIContent);

				Rect fieldRect = new Rect(combRect) { width = 80f, x = combRect.x + EditorGUIUtility.labelWidth + 50f};
				RuntimeSetting.CombFilteringPreventionInSeconds = EditorGUI.FloatField(fieldRect, RuntimeSetting.CombFilteringPreventionInSeconds);

				Rect defaultButtonRect = new Rect(fieldRect) { x = fieldRect.xMax + 10f, width = 60f};
				float defaultValue = Data.RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds;
				EditorGUI.BeginDisabledGroup(RuntimeSetting.CombFilteringPreventionInSeconds == defaultValue);
				if (GUI.Button(defaultButtonRect, "Default"))
				{
					RuntimeSetting.CombFilteringPreventionInSeconds = defaultValue;
				}
				EditorGUI.EndDisabledGroup();

				RuntimeSetting.LogCombFilteringWarning = EditorGUI.Toggle(GetRectAndIterateLine(drawPosition),"Log Warning If Occurs", RuntimeSetting.LogCombFilteringWarning);
				DrawEmptyLine(1);
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

			EditorSetting.ShowAudioTypeOnAudioID = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowAudioTypeToggleLabel, EditorSetting.ShowAudioTypeOnAudioID);

			if (EditorSetting.ShowAudioTypeOnAudioID)
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

		private void DrawInfo(Rect drawPosition)
		{
            EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Info".ToWhiteBold().SetSize(20), GUIStyleHelper.MiddleCenterRichText);
            drawPosition.x += drawPosition.width * 0.1f;
            drawPosition.xMax -= drawPosition.width * 0.2f;
            DrawEmptyLine(1);
            EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), _instruction.GetText(Instruction.Copyright), GUIStyleHelper.MiddleCenterText);

			DrawEmptyLine(1);
			DrawUrlLink(GetRectAndIterateLine(drawPosition), GitURL, TextAnchor.MiddleCenter);

			DrawEmptyLine(1);
			Rect resetButtonRect = GetRectAndIterateLine(drawPosition);
            resetButtonRect.height *= 1.2f;
            if (GUI.Button(resetButtonRect, ResetSettingButtonText))
			{
				RuntimeSetting.ResetToFactorySettings();
				EditorSetting.ResetToFactorySettings();
			}

            DrawEmptyLine(2);
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Demo Credits".ToWhiteBold().SetSize(20),GUIStyleHelper.MiddleCenterRichText);
            DrawEmptyLine(1);
            DrawAssetCredits(drawPosition);
        }

        private void DrawAssetCredits(Rect drawPosition)
        {
			if(_creditsObjects == null)
			{
                _creditsObjects = Resources.LoadAll("Editor", typeof(AssetCredits));
				_creditsObjects = _creditsObjects ?? Array.Empty<UnityEngine.Object>();
            }

			foreach(var obj in _creditsObjects)
			{
				if(obj is AssetCredits creditsObj)
				{
                    foreach (var credit in creditsObj.Credits)
                    {
                        if (Event.current.type == EventType.Repaint)
                        {
                            Rect boxRect = GetNextLineRect(this, drawPosition);
							boxRect.y -= 10f;
							boxRect.height = SingleLineSpace * CreditsFieldCount + 10f;
                            GUI.skin.box.Draw(boxRect, false, false, false, false);
                        }
                        EditorGUI.ObjectField(GetRectAndIterateLine(drawPosition), credit.Source, typeof(AudioClip), false);
						DrawSelectableLabelWithPrefix(GetRectAndIterateLine(drawPosition), new GUIContent("Type"), credit.Type.ToString(), CreditsPrefixWidth);
						DrawSelectableLabelWithPrefix(GetRectAndIterateLine(drawPosition), new GUIContent("Name"), credit.Name, CreditsPrefixWidth);
                        DrawSelectableLabelWithPrefix(GetRectAndIterateLine(drawPosition), new GUIContent("Author") ,credit.Author, CreditsPrefixWidth);
                        DrawUrlLink(GetRectAndIterateLine(drawPosition), credit.Link);
                        DrawEmptyLine(1);
                    }
                }
			}
        }

        private void DrawAssetOutputPath(Rect drawPosition)
		{
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AssetOutputPathLabel, GUIStyleHelper.MiddleCenterRichText);

			GUIStyle style = new GUIStyle(EditorStyles.objectField);
			style.alignment = TextAnchor.MiddleCenter;
			Rect rect = GetRectAndIterateLine(drawPosition);
			rect.x += drawPosition.width * 0.15f;
			rect.width = drawPosition.width * 0.7f;
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