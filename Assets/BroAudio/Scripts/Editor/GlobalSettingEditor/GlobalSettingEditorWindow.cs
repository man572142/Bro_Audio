using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.BroAudio.Utility;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.BroName;
using static Ami.BroAudio.BroLog;
using System.IO;
using UnityEngine.Audio;
using System;
using System.Linq;

namespace Ami.BroAudio.Editor.Setting
{
	public class GlobalSettingEditorWindow : MiEditorWindow
	{
		public enum OpenMessage
		{
			None,
			Welcome,
			SettingAssetFileMissing,
		}

		public enum Tab
		{
			Audio,
			GUI,
			Info,
		}

		public const string SettingFileName = "BroAudioGlobalSetting";
		public const string SettingText = "Setting";
		public const float Gap = 50f;
		public const string BrowserIcon = "FolderOpened Icon";
		
		public const string HaasEffectLabel = "Time to prevent Comb Filtering (Haas Effect)";
		public const string ResetSettingButtonText = "Reset To Factory Settings";
		public const string AutoMatchTracksButtonText = "Auto-adding tracks to match audio voices.";
		public const string AssetOutputPathLabel = "Asset Output Path";
		public const string VUColorToggleLabel = "Show VU color on volume slider";
		public const string ShowAudioTypeToggleLabel = "Show audioType on AudioID";
		public const string AudioTypeColorLabel = "Audio Type Color";
		public const string GitURL = "https://github.com/man572142/Bro_Audio";
		
		public const string ProjectSettingsMenuItemPath = "Edit/" + ProjectSettings;
		public const string ProjectSettings = "Project Settings";
		public const string RealVoicesParameterName = "Max Real Voices";

		private readonly string _titleText = nameof(BroAudio).ToBold().SetSize(30).SetColor(MainTitleColor);

		private GUIContent[] _tabs = null;
		private Tab _currentSelectTab = Tab.Audio;
		private int _currProjectSettingVoiceCount = -1;
		private int _currentMixerTracksCount = -1;
		private GUIContent _haasEffectGUIContent = null;
		private GUIContent _audioVoicesGUIContent = null;
		private BroInstructionHelper _instruction = new BroInstructionHelper();
		private AudioMixerGroup _duplicateTrackSource = null;
		private AudioMixer _mixer = null;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;
		public OpenMessage Message { get; private set; } = OpenMessage.None;
		
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
			window.titleContent = new GUIContent(SettingText);
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
			_instruction.Init();
			InitTabs();
			InitGUIContents();
		}

		

		private void InitGUIContents()
		{
			_haasEffectGUIContent ??= new GUIContent();
			_haasEffectGUIContent.text = HaasEffectLabel;
			_haasEffectGUIContent.tooltip = _instruction.GetText(Instruction.HaasEffectTooltip);

			_audioVoicesGUIContent ??= new GUIContent();
			_audioVoicesGUIContent.text = RealVoicesParameterName;
			_audioVoicesGUIContent.tooltip = _instruction.GetText(Instruction.AudioVoicesToolTip);
		}

		private void OnDisable()
		{
			ResetTracksAndAudioVoices();
		}

		private void ResetTracksAndAudioVoices()
		{
			_currProjectSettingVoiceCount = -1;
			_currentMixerTracksCount = -1;
		}

		private void InitTabs()
		{
			_tabs ??= new GUIContent[3];
			_tabs[(int)Tab.Audio] = EditorGUIUtility.IconContent("d_AudioMixerController On Icon");
			_tabs[(int)Tab.GUI] = EditorGUIUtility.IconContent("GUISkin Icon");
			_tabs[(int)Tab.Info] = EditorGUIUtility.IconContent("UnityEditor.InspectorWindow@2x");
		}


		protected override void OnGUI()
		{
			base.OnGUI();

			Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

			DrawEmptyLine(1);
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), _titleText, GUIStyleHelper.Instance.MiddleCenterRichText);

			DrawEmptyLine(1);

			switch (Message)
			{
				case OpenMessage.SettingAssetFileMissing:
					Rect errorRect = GetRectAndIterateLine(drawPosition);
					errorRect.height *= 2;
					EditorGUI.HelpBox(errorRect, _instruction.GetText(Instruction.SettingFileMissing), MessageType.Error);
					break;
				case OpenMessage.None:

					break;
			}

			DrawEmptyLine(1);
			DrawAssetOutputPath(drawPosition);

			DrawEmptyLine(1);

			drawPosition.x += Gap;
			drawPosition.width -= Gap * 2;

			DrawTabs(drawPosition);
			DrawEmptyLine(1);

			Rect tabBackgroundRect = GetRectAndIterateLine(drawPosition);
			tabBackgroundRect.y -= 6f;
			tabBackgroundRect.yMax = drawPosition.yMax;

			EditorGUI.indentLevel++;
			EditorGUI.DrawRect(tabBackgroundRect, UnityDefaultEditorColor);

			if (GlobalSetting != null)
			{
				switch (_currentSelectTab)
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
		}

		private void DrawTabs(Rect drawPosition)
		{
			Rect tabRect = GetRectAndIterateLine(drawPosition);
			tabRect.height = EditorGUIUtility.singleLineHeight * 2f;
			_currentSelectTab = (Tab)GUI.Toolbar(tabRect, (int)_currentSelectTab, _tabs);
		}

		private void DrawAudioSetting(Rect drawPosition)
		{
			DrawHaasEffectSetting(drawPosition);
			DrawEmptyLine(1);

			DrawDefaultEasing(drawPosition);
			DrawSeamlessLoopEasing(drawPosition);
			DrawEmptyLine(1);
			DrawAudioProjectSettings(drawPosition);

			void DrawDefaultEasing(Rect drawPosition)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Default Easing".ToWhiteBold(), GUIStyleHelper.Instance.RichText);
				EditorGUI.indentLevel++;
				GlobalSetting.DefaultFadeInEase =
					(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", GlobalSetting.DefaultFadeInEase);
				GlobalSetting.DefaultFadeOutEase =
					(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", GlobalSetting.DefaultFadeOutEase);
				EditorGUI.indentLevel--;
			}

			void DrawSeamlessLoopEasing(Rect drawPosition)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Seamless Loop Easing".ToWhiteBold(), GUIStyleHelper.Instance.RichText);
				EditorGUI.indentLevel++;
				GlobalSetting.SeamlessFadeInEase =
					(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", GlobalSetting.SeamlessFadeInEase);
				GlobalSetting.SeamlessFadeOutEase =
					(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", GlobalSetting.SeamlessFadeOutEase);
				EditorGUI.indentLevel--;
			}

			void DrawHaasEffectSetting(Rect drawPosition)
			{
				Rect haasRect = GetRectAndIterateLine(drawPosition);
				EditorGUI.LabelField(haasRect, _haasEffectGUIContent);

				haasRect.width *= 0.5f;
				haasRect.x += 150f;
				GlobalSetting.HaasEffectInSeconds = EditorGUI.FloatField(haasRect, " ", GlobalSetting.HaasEffectInSeconds);
			}

			void DrawAudioProjectSettings(Rect drawPosition)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), ProjectSettings.ToWhiteBold(), GUIStyleHelper.Instance.RichText);

				if (HasValidProjectSettingVoiceCount())
				{
					EditorGUI.BeginDisabledGroup(true);
					{
						Rect voiceRect = GetRectAndIterateLine(drawPosition);
						EditorGUI.LabelField(voiceRect, _audioVoicesGUIContent);
						voiceRect.x += 150f;
						voiceRect.width = 100f;
						EditorGUI.IntField(voiceRect, _currProjectSettingVoiceCount);
					}
					EditorGUI.EndDisabledGroup();
				}

				if (HasValidMixerTracksCount() && _currentMixerTracksCount < _currProjectSettingVoiceCount)
				{
					Rect warningBoxRect = GetRectAndIterateLine(drawPosition);
					warningBoxRect.height *= 3;
					Color linkBlue = EditorStyles.linkLabel.normal.textColor;
					string text = string.Format(_instruction.GetText(Instruction.TracksAndVoicesNotMatchWarning), MixerName.ToWhiteBold(), RealVoicesParameterName.ToWhiteBold(), ProjectSettingsMenuItemPath.SetColor(linkBlue));
					RichTextHelpBox(warningBoxRect, text, MessageType.Warning);
					if (GUI.Button(warningBoxRect, GUIContent.none, GUIStyle.none))
					{
						SettingsService.OpenProjectSettings(AudioSettingPath);
					}
					EditorGUIUtility.AddCursorRect(warningBoxRect, MouseCursor.Link);

					Rect autoMatchBtnRect = GetRectAndIterateLine(drawPosition);
					autoMatchBtnRect.y += EditorGUIUtility.singleLineHeight * 2f;
					autoMatchBtnRect.height *= 2f;
					if (GUI.Button(autoMatchBtnRect, AutoMatchTracksButtonText) 
						&& EditorUtility.DisplayDialog("Confirmation", _instruction.GetText(Instruction.AddTracksConfirmationDialog), "OK", "Cancel"))
					{
						AutoMatchAudioVoices();
					}
				}
			}
		}

		private void AutoMatchAudioVoices()
		{
			AudioMixerGroup mainTrack = AudioMixer.FindMatchingGroups(MainTrackName)?.FirstOrDefault();
			if (mainTrack == default || _currentMixerTracksCount == default)
			{
				LogError("Can't get the Main track or other BroAudio track");
				return;
			}

			if(_duplicateTrackSource)
			{
				for(int i = _currentMixerTracksCount +1 ; i <= _currProjectSettingVoiceCount; i++)
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
			GlobalSetting.ShowVUColorOnVolumeSlider = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), VUColorToggleLabel, GlobalSetting.ShowVUColorOnVolumeSlider);
			DemonstrateSlider(drawPosition);

			GlobalSetting.ShowAudioTypeOnAudioID = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), ShowAudioTypeToggleLabel, GlobalSetting.ShowAudioTypeOnAudioID);

			if (GlobalSetting.ShowAudioTypeOnAudioID)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AudioTypeColorLabel.ToWhiteBold(), GUIStyleHelper.Instance.RichText);
				EditorGUI.indentLevel++;
				Rect colorRect = GetRectAndIterateLine(drawPosition);
				colorRect.xMax -= 20f;
				SplitRectHorizontal(colorRect, 0.5f, 0f, out Rect leftColorRect, out Rect rightColorRect);
				int count = 0;
				ForeachAudioType((audioType) =>
				{
					if (audioType != BroAudioType.None && audioType != BroAudioType.All)
					{
						SetAudioTypeLabelColor(count % 2 == 0 ? leftColorRect : rightColorRect, audioType);
						count++;
						if (count % 2 == 1)
						{
							leftColorRect.y += SingleLineSpace;
							rightColorRect.y += SingleLineSpace;
						}
					}
				});
			}

			void DemonstrateSlider(Rect drawPosition)
			{
				Rect sliderRect = GetRectAndIterateLine(drawPosition);
				sliderRect.width = drawPosition.width * 0.5f;
				sliderRect.x += Gap;
				if (GlobalSetting.ShowVUColorOnVolumeSlider)
				{
					Rect vuRect = new Rect(sliderRect);
					vuRect.height *= 0.5f;
					EditorGUI.DrawTextureTransparent(vuRect, EditorGUIUtility.IconContent("d_VUMeterTextureHorizontal").image);
                    EditorGUI.DrawRect(vuRect, VUMaskColor);
                }
				GUI.HorizontalSlider(sliderRect, 1f, 0f, 1.25f);
			}

			void SetAudioTypeLabelColor(Rect fieldRect, BroAudioType audioType)
			{
				switch (audioType)
				{
					case BroAudioType.Music:
						GlobalSetting.MusicColor = EditorGUI.ColorField(fieldRect, audioType.ToString(), GlobalSetting.MusicColor);
						break;
					case BroAudioType.UI:
						GlobalSetting.UIColor = EditorGUI.ColorField(fieldRect, audioType.ToString(), GlobalSetting.UIColor);
						break;
					case BroAudioType.Ambience:
						GlobalSetting.AmbienceColor = EditorGUI.ColorField(fieldRect, audioType.ToString(), GlobalSetting.AmbienceColor);
						break;
					case BroAudioType.SFX:
						GlobalSetting.SFXColor = EditorGUI.ColorField(fieldRect, audioType.ToString(), GlobalSetting.SFXColor);
						break;
					case BroAudioType.VoiceOver:
						GlobalSetting.VoiceOverColor = EditorGUI.ColorField(fieldRect, audioType.ToString(), GlobalSetting.VoiceOverColor);
						break;
					default:
						break;
				}
			}
		}

		private void DrawInfo(Rect drawPosition)
		{
            DrawEmptyLine(2);
            EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), _instruction.GetText(Instruction.Copyright), GUIStyleHelper.Instance.MiddleCenterText);

			DrawEmptyLine(1);
			var linkStyle = new GUIStyle(EditorStyles.linkLabel);
			linkStyle.alignment = TextAnchor.MiddleCenter;
			Rect linkRect = GetRectAndIterateLine(drawPosition);
			if (GUI.Button(linkRect, GitURL, linkStyle))
			{
				Application.OpenURL(GitURL);
			}
			EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);

			DrawEmptyLine(1);
            if (GUI.Button(GetRectAndIterateLine(drawPosition),ResetSettingButtonText))
			{
				GlobalSetting.ResetToFactorySettings();
			}
		}

		private void DrawAssetOutputPath(Rect drawPosition)
		{
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), AssetOutputPathLabel, GUIStyleHelper.Instance.MiddleCenterRichText);

			GUIStyle style = EditorStyles.objectField;
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
				if (!string.IsNullOrEmpty(newPath))
				{
					if (IsInProjectFolder(newPath))
					{
						AssetOutputPath = newPath.Remove(0, UnityProjectRootPath.Length + 1);
						WriteAssetOutputPathToCoreData();
					}
				}
			}
			Rect browserIconRect = rect;
			browserIconRect.width = EditorGUIUtility.singleLineHeight;
			browserIconRect.height = EditorGUIUtility.singleLineHeight;
			browserIconRect.x = rect.xMax - EditorGUIUtility.singleLineHeight;
			GUI.DrawTexture(browserIconRect, EditorGUIUtility.IconContent(BrowserIcon).image);
			EditorGUI.DrawRect(browserIconRect, BroAudioGUISetting.ShadowMaskColor);
		}
	}
}