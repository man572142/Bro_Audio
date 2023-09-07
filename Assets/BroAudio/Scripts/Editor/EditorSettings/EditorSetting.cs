using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{

	public class EditorSetting : ScriptableObject
	{
		public bool ShowAudioTypeOnAudioID;
		public bool ShowVUColorOnVolumeSlider;

		public Color MusicColor;
		public Color UIColor;
		public Color AmbienceColor;
		public Color SFXColor;
		public Color VoiceOverColor;

		public IReadOnlyDictionary<BroAudioType, DrawedProperty> PropertySettings;

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

			PropertySettings = new Dictionary<BroAudioType, DrawedProperty>
			{
				{ BroAudioType.Music, FactorySettings.MusicDrawedProperties},
				{ BroAudioType.UI, FactorySettings.UIDrawedProperties},
				{ BroAudioType.Ambience, FactorySettings.AmbienceDrawedProperties},
				{ BroAudioType.SFX, FactorySettings.SFXDrawedProperties},
				{ BroAudioType.VoiceOver, FactorySettings.VoiceOverDrawedProperties},
			};

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

			public const DrawedProperty BasicDrawedProperty = DrawedProperty.Volume | DrawedProperty.PlaybackPosition | DrawedProperty.Fade | DrawedProperty.ClipPreview;

			public const DrawedProperty MusicDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop | DrawedProperty.SeamlessLoop;
			public const DrawedProperty UIDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay;
			public const DrawedProperty AmbienceDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop | DrawedProperty.SeamlessLoop;
			public const DrawedProperty SFXDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay | DrawedProperty.Loop | DrawedProperty.SeamlessLoop;
			public const DrawedProperty VoiceOverDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay;

			public const bool ShowVUColorOnVolumeSlider = true;
		}
	}
}