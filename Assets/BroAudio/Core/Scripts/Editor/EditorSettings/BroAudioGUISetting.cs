using Ami.BroAudio.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor.Setting
{
	public static class BroAudioGUISetting
	{
		// Unity adds separator every 11 element

		public const int LibraryManagerMenuIndex = 0;
		public const string LibraryManagerMenuPath = BroName.MenuItem_BroAudio + BroName.MenuItem_LibraryManager;

        public const int PreferencesMenuIndex = 11;
		public const string PreferencesMenuPath = BroName.MenuItem_BroAudio + BroName.MenuItem_Preferences;

		public const int InfoWindowMenuIndex = 12;
		public const string InfoWindowMenuPath = BroName.MenuItem_BroAudio + BroName.MenuItem_Info;

		public const int ClipEditorMenuIndex = 23;
		public const string ClipEditorMenuPath = BroName.MenuItem_BroAudio + BroName.MenuItem_ClipEditor;

		public const int AudioEffectEditorMenuIndex = 24;
		public const string AudioEffectMenuPath = BroName.MenuItem_BroAudio + BroName.MenuItem_EffectEditor;

		public const int DevToolsMenuIndex = 35;

		public static Color MainTitleColor => new Color(0.04f, 0.82f, 0.7f);
		public static Color ClipLabelColor => new Color(0f, 0.9f, 0.5f);
		public static Color PlayButtonColor => new Color(0.25f, 0.9f, 0.25f, 0.4f);
		public static Color StopButtonColor => new Color(0.9f, 0.25f, 0.25f, 0.4f);
		public static Color ShadowMaskColor => new Color(0.05f, 0.05f, 0.05f, 0.3f);
		public static Color VUMaskColor => new Color(0.2f, 0.2f, 0.2f, 0.6f);
		public static Color FalseColor => new Color(0.9f, 0.3f, 0.3f, 1f);
		public static Color DefaultLabelColor => GUI.skin.label.normal.textColor;
        public static Color SelectedColor => new Color(0.17f, 0.36f, 0.52f, 1f);

        public static Vector2 DefaultWindowSize => new Vector2(720f, 540f);
	}
}