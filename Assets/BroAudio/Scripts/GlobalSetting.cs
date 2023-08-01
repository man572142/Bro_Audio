using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;
using UnityEditor;
using Codice.CM.Interfaces;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(menuName = "BroAudio(DevOnly)/Create Global Setting Asset File", fileName = "BroAudioGlobalSetting")]
	public class GlobalSetting : ScriptableObject
	{
		public class FactorySettings
		{
			public const float HaasEffectInSeconds = 0.04f;
			public const Ease DefaultFadeInEase = Ease.InCubic;
			public const Ease DefaultFadeOutEase = Ease.OutSine;
			public const Ease SeamlessFadeInEase = Ease.OutCubic;
			public const Ease SeamlessFadeOutEase = Ease.OutSine;

			public const int DefaultAudioPlayerPoolSize = 5;

			public const bool ShowAudioTypeOnAudioID = true;

			public const string MusicColor = "#001A4C4C";
			public const string UIColor = "#0080334C";
			public const string AmbienceColor = "#1A80804C";
			public const string SFXColor = "#B233334C";
			public const string VoiceOverColor = "#CC99004C";

			public const bool ShowVUColorOnVolumeSlider = true;
		}

		public const string FileName = "BroAudioGlobalSetting";

		public float HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;

#if UNITY_EDITOR
		public bool ShowAudioTypeOnAudioID = FactorySettings.ShowAudioTypeOnAudioID;

		public Color MusicColor = new Color(0f, 0.1f, 0.3f, 0.3f);
		public Color UIColor = new Color(0f, 0.5f, 0.2f, 0.3f);
		public Color AmbienceColor = new Color(0.1f, 0.5f, 0.5f, 0.3f);
		public Color SFXColor = new Color(0.7f, 0.2f, 0.2f, 0.3f);
		public Color VoiceOverColor = new Color(0.8f, 0.6f, 0f, 0.3f);

		public bool ShowVUColorOnVolumeSlider = FactorySettings.ShowVUColorOnVolumeSlider;

		public Color GetAudioTypeColor(BroAudioType audioType)
		{
			return audioType switch
			{
				BroAudioType.Music => MusicColor,
				BroAudioType.UI => UIColor,
				BroAudioType.Ambience => AmbienceColor,
				BroAudioType.SFX => SFXColor,
				BroAudioType.VoiceOver => VoiceOverColor,
				_ => default,
			};
		}

		public void ResetToFactorySettings()
		{
			HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;

            ShowVUColorOnVolumeSlider = FactorySettings.ShowVUColorOnVolumeSlider;
            ShowAudioTypeOnAudioID = FactorySettings.ShowAudioTypeOnAudioID;

			if(ColorUtility.TryParseHtmlString(FactorySettings.MusicColor, out var musicColor))
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
#endif
    }

}