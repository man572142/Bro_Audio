using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class EditorSetting : ScriptableObject
	{
		private Dictionary<BroAudioType, DrawedProperty> _propertySettings = new Dictionary<BroAudioType, DrawedProperty>
		{
			{ BroAudioType.Music, default},
			{ BroAudioType.UI, default},
			{ BroAudioType.Ambience, default},
			{ BroAudioType.SFX, default},
			{ BroAudioType.VoiceOver, default},
		};

		public IReadOnlyDictionary<BroAudioType, DrawedProperty> PropertySettings => _propertySettings;

		public bool ShowAudioTypeOnAudioID = FactorySettings.ShowAudioTypeOnAudioID;

		public Color MusicColor = new Color(0f, 0.1f, 0.3f, 0.3f);
		public Color UIColor = new Color(0f, 0.5f, 0.2f, 0.3f);
		public Color AmbienceColor = new Color(0.1f, 0.5f, 0.5f, 0.3f);
		public Color SFXColor = new Color(0.7f, 0.2f, 0.2f, 0.3f);
		public Color VoiceOverColor = new Color(0.8f, 0.6f, 0f, 0.3f);

		public bool ShowVUColorOnVolumeSlider = FactorySettings.ShowVUColorOnVolumeSlider;

		public Color GetAudioTypeColor(BroAudioType audioType)
		{
			switch (audioType)
			{
				case BroAudioType.Music:
					return MusicColor;
				case BroAudioType.UI:
					return UIColor;
				case BroAudioType.Ambience:
					return AmbienceColor;
				case BroAudioType.SFX:
					return SFXColor;
				case BroAudioType.VoiceOver:
					return VoiceOverColor;
				default:
					return default;
			}
		}

		public void ResetToFactorySettings()
		{
			ShowVUColorOnVolumeSlider = FactorySettings.ShowVUColorOnVolumeSlider;
			ShowAudioTypeOnAudioID = FactorySettings.ShowAudioTypeOnAudioID;

			if (ColorUtility.TryParseHtmlString(FactorySettings.MusicColor, out var musicColor))
			{
				MusicColor = musicColor;
			}

			if (ColorUtility.TryParseHtmlString(FactorySettings.UIColor, out var uiColor))
			{
				UIColor = uiColor;
			}

			if (ColorUtility.TryParseHtmlString(FactorySettings.AmbienceColor, out var ambColor))
			{
				AmbienceColor = ambColor;
			}

			if (ColorUtility.TryParseHtmlString(FactorySettings.SFXColor, out var sfxColor))
			{
				SFXColor = sfxColor;
			}
			if (ColorUtility.TryParseHtmlString(FactorySettings.VoiceOverColor, out var voiceColor))
			{
				VoiceOverColor = voiceColor;
			}
		}

		public class FactorySettings
		{
			public const bool ShowAudioTypeOnAudioID = true;

			public const string MusicColor = "#001A4C4C";
			public const string UIColor = "#0080334C";
			public const string AmbienceColor = "#1A80804C";
			public const string SFXColor = "#B233334C";
			public const string VoiceOverColor = "#CC99004C";

			public const bool ShowVUColorOnVolumeSlider = true;
		}
	}
}