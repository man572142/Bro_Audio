using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using static MiProduction.BroAudio.Editor.BroEditorUtility;
using static MiProduction.BroAudio.Editor.Setting.BroAudioGUISetting;
using static MiProduction.Extension.EditorScriptingExtension;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.Editor.Setting
{
	public class GlobalSettingEditorWindow : MiEditorWindow
	{
		public enum OpenMessage
		{
			None,
			Welcome,
			CoreDataMissing,
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
		private const string CoreDataMissingText = "Core data is missing! please relocate the Root Path to where [BroAudioData.json] is located";
		private const string HaasEffectInfoText =
				"If the same sound is played repeatedly in a very short period of time (e.g., playing it every other frame). " +
				"It may cause some quality loss or unexpected behavior due to the nature of Comb Filtering (or Hass Effect). " +
				"You can set it to 0 to ignore this feature, or any other shorter value as needed.";

		private readonly string _titleText = nameof(BroAudio).ToBold().SetSize(30).SetColor(BroAudioGUISetting.MainTitleColor);

		private GlobalSetting _setting = null;
		private GUIContent[] _tabs = null;
		private Tab _currentSelectTab = Tab.Audio;

		public override float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

		public OpenMessage Message { get; private set; } = OpenMessage.None;


		[MenuItem(GlobalSettingMenuPath,false,GlobalSettingMenuIndex)]
		public static GlobalSettingEditorWindow ShowWindow()
		{
			EditorWindow window = GetWindow(typeof(GlobalSettingEditorWindow));
			window.minSize = new Vector2(640f, 360f);
			window.titleContent = new GUIContent(SettingText);
			window.Show();
			return window as GlobalSettingEditorWindow;
		}

		public static void OpenWithMessage(OpenMessage message)
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
			_setting = Resources.Load<GlobalSetting>(SettingFileName);
			if(_setting == null)
			{
				Message = OpenMessage.CoreDataMissing;
			}
		}

		private void InitTabs()
		{
			_tabs ??= new GUIContent[3];
			_tabs[(int)Tab.Audio] = EditorGUIUtility.IconContent("Audio Mixer@2x");
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
				case OpenMessage.CoreDataMissing:
					Rect errorRect = GetRectAndIterateLine(drawPosition);
					errorRect.height *= 2;
					EditorGUI.HelpBox(errorRect, CoreDataMissingText, MessageType.Error);
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
			//drawPosition.width = 400f;
			Rect haasRect = GetRectAndIterateLine(drawPosition);
			EditorGUI.LabelField(haasRect, "Time to prevent Comb Filtering (Haas Effect)");

			haasRect.width *= 0.5f;
			haasRect.x += 150f;
			_setting.HaasEffectInSeconds =
				EditorGUI.FloatField(haasRect, " ", _setting.HaasEffectInSeconds);

			Rect haasInfoRect = GetRectAndIterateLine(drawPosition);
			haasInfoRect.height *= 3f;
			EditorGUI.HelpBox(haasInfoRect,HaasEffectInfoText, MessageType.Info);

			DrawEmptyLine(2);

			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Default Easing".ToWhiteBold(),GUIStyleHelper.Instance.RichText);
			EditorGUI.indentLevel++;
			_setting.DefaultFadeInEase = 
				(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", _setting.DefaultFadeInEase);
			_setting.DefaultFadeOutEase = 
				(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", _setting.DefaultFadeOutEase);
			EditorGUI.indentLevel--;

			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), "Seamless Loop Easing".ToWhiteBold(), GUIStyleHelper.Instance.RichText);
			EditorGUI.indentLevel++;
			_setting.SeamlessFadeInEase =
				(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade In", _setting.SeamlessFadeInEase);
			_setting.SeamlessFadeOutEase =
				(Ease)EditorGUI.EnumPopup(GetRectAndIterateLine(drawPosition), "Fade Out", _setting.SeamlessFadeOutEase);
			EditorGUI.indentLevel--;
		}

		private void DrawGUISetting(Rect drawPosition)
		{
			_setting.ShowAudioTypeOnAudioID = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), "Show audioType on AudioID", _setting.ShowAudioTypeOnAudioID);
			_setting.ShowVUColorOnVolumeSlider = EditorGUI.ToggleLeft(GetRectAndIterateLine(drawPosition), "Show VU color on volume slider", _setting.ShowVUColorOnVolumeSlider);

		}

		private void DrawInfo(Rect drawPosition)
		{

			EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), "Copyright BroAudio man572142");
			EditorGUI.SelectableLabel(GetRectAndIterateLine(drawPosition), "https://github.com/man572142/Bro_Audio");
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