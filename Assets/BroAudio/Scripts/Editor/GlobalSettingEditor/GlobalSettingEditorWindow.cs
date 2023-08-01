using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using static Ami.BroAudio.Utility;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Editor.Setting.BroAudioGUISetting;
using static Ami.Extension.EditorScriptingExtension;
using static Ami.BroAudio.BroLog;

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
		private const string SettingText = "Setting";
		public const float Gap = 50f;
		private const string BrowserIcon = "FolderOpened Icon";
		private const string AssetOutputPathPanelTtile = "Select BroAudio auto-generated asset file's output folder";
		private const string SettingFileMissingText = "Setting asset file is missing! please relocate the file to any [Resource] folder or recreate a new one";
		private const string HaasEffectInfoText =
				"If the same sound is played repeatedly in a very short period of time (e.g. playing it every other frame). " +
				"It may cause some quality loss or unexpected behavior due to the nature of Comb Filtering (or Hass Effect). " +
				"You can set it to 0 to ignore this feature, or any other shorter value as needed.";
		private const string GitURL = "https://github.com/man572142/Bro_Audio";
		private const string CopyrightText = "Copyright 2022-2023 CheHsiang Weng.";
		private const string AllRightsReserved = "All rights reserved.";

        private readonly string _titleText = nameof(BroAudio).ToBold().SetSize(30).SetColor(MainTitleColor);

		private GUIContent[] _tabs = null;
		private Tab _currentSelectTab = Tab.Audio;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

		public OpenMessage Message { get; private set; } = OpenMessage.None;


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
			InitTabs();
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
					EditorGUI.HelpBox(errorRect, SettingFileMissingText, MessageType.Error);
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
		private void DrawAudioSetting(Rect drawPosition)
		{
			Rect haasRect = GetRectAndIterateLine(drawPosition);
			EditorGUI.LabelField(haasRect, "Time to prevent Comb Filtering (Haas Effect)");

			haasRect.width *= 0.5f;
			haasRect.x += 150f;
			GlobalSetting.HaasEffectInSeconds =
				EditorGUI.FloatField(haasRect, " ", GlobalSetting.HaasEffectInSeconds);

			Rect haasInfoRect = GetRectAndIterateLine(drawPosition);
			haasInfoRect.height *= 3f;
			EditorGUI.HelpBox(haasInfoRect, HaasEffectInfoText, MessageType.Info);

			DrawEmptyLine(2);
			DrawDefaultEasing(drawPosition);
			DrawSeamlessLoopEasing(drawPosition);

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
		}

		private void DrawGUISetting(Rect drawPosition)
		{
			GlobalSetting.ShowVUColorOnVolumeSlider = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), "Show VU color on volume slider", GlobalSetting.ShowVUColorOnVolumeSlider);
			DemonstrateSlider(drawPosition);

			GlobalSetting.ShowAudioTypeOnAudioID = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), "Show audioType on AudioID", GlobalSetting.ShowAudioTypeOnAudioID);

			if (GlobalSetting.ShowAudioTypeOnAudioID)
			{
				EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Audio Type Color".ToWhiteBold(), GUIStyleHelper.Instance.RichText);
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
		}

		private void SetAudioTypeLabelColor(Rect fieldRect, BroAudioType audioType)
		{
			switch (audioType)
			{
				case BroAudioType.Music:
                    GlobalSetting.MusicColor = EditorGUI.ColorField(fieldRect,audioType.ToString(), GlobalSetting.MusicColor);
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

		private void DrawInfo(Rect drawPosition)
		{
            DrawEmptyLine(2);
            EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), CopyrightText, GUIStyleHelper.Instance.MiddleCenterText);
            EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), AllRightsReserved, GUIStyleHelper.Instance.MiddleCenterText);

			DrawEmptyLine(1);
			var linkStyle = new GUIStyle(EditorStyles.linkLabel);
			linkStyle.alignment = TextAnchor.MiddleCenter;
            if (GUI.Button(GetRectAndIterateLine(drawPosition), GitURL, linkStyle))
			{
				Application.OpenURL(GitURL);
			}

            DrawEmptyLine(1);
            if (GUI.Button(GetRectAndIterateLine(drawPosition),"Reset To Factory Settings"))
			{
				GlobalSetting.ResetToFactorySettings();
			}
		}


		private void DrawTabs(Rect drawPosition)
		{
			Rect tabRect = GetRectAndIterateLine(drawPosition);
			tabRect.height = EditorGUIUtility.singleLineHeight * 2f;
			_currentSelectTab = (Tab)GUI.Toolbar(tabRect, (int)_currentSelectTab, _tabs);
		}

		private void DrawAssetOutputPath(Rect drawPosition)
		{
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Asset Output Path", GUIStyleHelper.Instance.MiddleCenterRichText);

			GUIStyle style = EditorStyles.objectField;
			style.alignment = TextAnchor.MiddleCenter;
			Rect rect = GetRectAndIterateLine(drawPosition);
			rect.x += drawPosition.width * 0.15f;
			rect.width = drawPosition.width * 0.7f;
			if (GUI.Button(rect, new GUIContent(AssetOutputPath), style))
			{
				string openPath = AssetOutputPath;
				if (!System.IO.Directory.Exists(GetFullPath(openPath)))
				{
					openPath = Application.dataPath;
				}
				string newPath = UnityEditor.EditorUtility.OpenFolderPanel(AssetOutputPathPanelTtile,openPath , "");
				if (!string.IsNullOrEmpty(newPath))
				{
					if (IsInProjectFolder(newPath))
					{
						AssetOutputPath = newPath.Remove(0, UnityAssetsRootPath.Length + 1);
						WriteAssetOutputPathToCoreData();
					}
					else
					{
						LogError("You cannot set path outside of Unity project's root folder!");
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


		private void DrawEaseOption(Rect drawPosition)
		{
			Keyframe[] keyframes = new Keyframe[2];
			keyframes[0] = new Keyframe(0, 0);
			keyframes[1] = new Keyframe(1, 1);

			keyframes[0].outTangent = 1f;
			keyframes[1].inTangent = 1f;
			var curve = new AnimationCurve(keyframes);

			EditorGUI.CurveField(GetRectAndIterateLine(drawPosition), new AnimationCurve(keyframes));
		}
	}

}