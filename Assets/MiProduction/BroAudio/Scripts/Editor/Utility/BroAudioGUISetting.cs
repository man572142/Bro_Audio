using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Editor.Setting
{
	public static class BroAudioGUISetting
	{
		public const int LibraryManagerMenuIndex = 0;
		public const string LibraryManagerMenuPath = "BroAudio/Library Manager";

		public const int ClipEditorMenuIndex = 1;
		public const string ClipEditorMenuPath = "BroAudio/Clip Editor";

		public const int GlobalSettingMenuIndex = 2;
		public const string GlobalSettingMenuPath = "BroAudio/Setting";

		public static Color MainTitleColor => new Color(0.04f, 0.82f, 0.7f);
		public static Color ClipLabelColor => new Color(0f, 0.9f, 0.5f);
		public static Color PlayButtonColor => new Color(0.25f, 0.9f, 0.25f, 0.4f);
		public static Color StopButtonColor => new Color(0.9f, 0.25f, 0.25f, 0.4f);
		public static Color ShadowMaskColor => new Color(0.05f, 0.05f, 0.05f, 0.3f);


		public static Vector2 MinWindowSize => new Vector2(960f, 540f);
		public static Vector2 UpdateButtonSize => new Vector2(80f, 40f);

		public static Color DarkBlue => new Color(0f, 0.1f, 0.3f, 0.3f);
		public static Color GrassGreen => new Color(0f, 0.5f, 0.2f, 0.3f);
		public static Color LakeGreen => new Color(0.1f, 0.5f, 0.5f, 0.3f);
		public static Color SoftRed => new Color(0.7f, 0.2f, 0.2f, 0.3f);
		public static Color Bronze => new Color(0.8f, 0.6f, 0f, 0.3f);

		public static Color UnityDefaultEditorColor => new Color(0.247f,0.247f,0.247f);

		public static Color GetAudioTypeColor(BroAudioType audioType)
		{
			switch (audioType)
			{
				case BroAudioType.Music:
					return DarkBlue;
				case BroAudioType.UI:
					return GrassGreen;
				case BroAudioType.Ambience:
					return LakeGreen;
				case BroAudioType.SFX:
					return SoftRed;
				case BroAudioType.VoiceOver:
					return Bronze;
				default:
					return Color.white;
			}
		}
	}
}