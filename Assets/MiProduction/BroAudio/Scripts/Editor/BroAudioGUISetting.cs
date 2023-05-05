using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.EditorSetting
{
	public static class BroAudioGUISetting
	{
		public static Color MainTitleColor => new Color(0.04f, 0.82f, 0.7f);
		public static Color ClipLabelColor => new Color(0f, 0.9f, 0.5f);
		public static Color PlayButtonColor => new Color(0.25f, 0.9f, 0.25f, 0.4f);
		public static Color StopButtonColor => new Color(0.9f, 0.25f, 0.25f, 0.4f);
		public static Color WaveformMaskColor => new Color(0.05f, 0.05f, 0.05f, 0.3f);


		public static Vector2 MinWindowSize => new Vector2(960f, 540f);
		public static Vector2 UpdateButtonSize => new Vector2(80f, 40f);

		public static Color MusicColor => new Color(0f, 0.1f, 0.3f, 0.3f);
		public static Color UIColor => new Color(0f, 0.5f, 0.2f, 0.3f);
		public static Color AmbienceColor => new Color(0.1f, 0.5f, 0.5f, 0.3f);
		public static Color SFXColor => new Color(0.7f, 0.2f, 0.2f, 0.3f);
		public static Color VoiceoverColor => new Color(0.8f, 0.6f, 0f, 0.3f);

		public static Color UnityDefaultEditorColor => new Color(0.247f,0.247f,0.247f);
	}
}