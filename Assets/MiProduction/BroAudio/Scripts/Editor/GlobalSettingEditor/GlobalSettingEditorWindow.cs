using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using static MiProduction.BroAudio.Editor.Setting.BroAudioGUISetting;

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

		private const string SettingText = "Setting";
		public const float Gap = 50f;
		private const string BrowserIcon = "FolderOpened Icon";
		private const string AssetOutputPathPanelTtile = "Select BroAudio auto-generated asset file's output folder";
		private const string CoreDataMissingText = "Core data is missing! please relocate the Root Path to where [BroAudioData.json] is located";

		private readonly string _titleText = BroAudio.ProjectName.ToBold().SetSize(30).SetColor(BroAudioGUISetting.MainTitleColor);

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


		protected override void OnGUI()
		{
			base.OnGUI();
			Rect drawPosition = new Rect(Gap * 0.5f, 0f, position.width - Gap, position.height);

			DrawEmptyLine(1);
			EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), _titleText, GUIStyleHelper.Instance.MiddleCenterRichText);

			switch (Message)
			{
				case OpenMessage.CoreDataMissing:
					EditorGUI.LabelField(GetRectAndIterateLine(drawPosition), CoreDataMissingText.SetColor(BroAudioGUISetting.SoftRed), GUIStyleHelper.Instance.MiddleCenterRichText);
					break;
				case OpenMessage.None:
					DrawEmptyLine(1);
					break;
			}

			DrawEmptyLine(1);
			DrawAssetOutputPath(drawPosition);
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
				string newPath = EditorUtility.OpenFolderPanel(AssetOutputPathPanelTtile,openPath , "");
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
	}

}