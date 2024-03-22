using Ami.BroAudio.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = nameof(BroAudio) + "/Editor Setting", fileName = BroName.EditorSettingPath)]
#endif
	public class EditorSetting : ScriptableObject
	{
		[System.Serializable]
		public struct AudioTypeSetting
		{
			public BroAudioType AudioType;
			public Color Color;
			public DrawedProperty DrawedProperty;

			public AudioTypeSetting(BroAudioType audioType,string colorString, DrawedProperty drawedProperty)
			{
				AudioType = audioType;
				ColorUtility.TryParseHtmlString(colorString, out Color);
				DrawedProperty = drawedProperty;
			}
		}

		public bool ShowAudioTypeOnSoundID;
		public bool ShowVUColorOnVolumeSlider;
		public bool AcceptAudioMixerModificationIn2021;

		public List<AudioTypeSetting> AudioTypeSettings;
		public Object DemoScene = null; 

		public Color GetAudioTypeColor(BroAudioType audioType)
		{
			if(TryGetAudioTypeSetting(audioType,out var setting))
			{
				return setting.Color;
			}
			return default;
		}

		public bool TryGetAudioTypeSetting(BroAudioType audioType,out AudioTypeSetting result)
		{
			result = default;

			// For temp asset
			if(audioType == BroAudioType.None)
			{
				result = new AudioTypeSetting()
				{
					AudioType = audioType,
					Color = Setting.BroAudioGUISetting.FalseColor,
					DrawedProperty = DrawedProperty.All,
				};
				return true;
			}

			if(AudioTypeSettings == null)
			{
				CreateNewAudioTypeSettings();
			}

			foreach (var setting in AudioTypeSettings)
			{
				if(audioType == setting.AudioType)
				{
					result = setting;
					return true;
				}
			}
			return false;
		}

		public bool WriteAudioTypeSetting(BroAudioType audioType, AudioTypeSetting newSetting)
		{
			for(int i = 0; i < AudioTypeSettings.Count; i++)
			{
				if (audioType == AudioTypeSettings[i].AudioType)
				{
					AudioTypeSettings[i] = newSetting;
					return true;
				}
			}
			return false;
		}

		public void ResetToFactorySettings()
		{
			ShowVUColorOnVolumeSlider = FactorySettings.ShowVUColorOnVolumeSlider;
			ShowAudioTypeOnSoundID = FactorySettings.ShowAudioTypeOnSoundID;
			CreateNewAudioTypeSettings();
		}

		private void CreateNewAudioTypeSettings()
		{
			AudioTypeSettings = new List<AudioTypeSetting>
			{
				 new AudioTypeSetting(BroAudioType.Music, FactorySettings.MusicColor, FactorySettings.MusicDrawedProperties),
				 new AudioTypeSetting(BroAudioType.UI, FactorySettings.UIColor, FactorySettings.UIDrawedProperties),
				 new AudioTypeSetting(BroAudioType.Ambience, FactorySettings.AmbienceColor, FactorySettings.AmbienceDrawedProperties),
				 new AudioTypeSetting(BroAudioType.SFX, FactorySettings.SFXColor, FactorySettings.SFXDrawedProperties),
				 new AudioTypeSetting(BroAudioType.VoiceOver, FactorySettings.VoiceOverColor, FactorySettings.VoiceOverDrawedProperties),
			};
		}

		public class FactorySettings
		{
			public const bool ShowAudioTypeOnSoundID = true;

			public const string MusicColor = "#001A4C4C";
			public const string UIColor = "#0080334C";
			public const string AmbienceColor = "#1A80804C";
			public const string SFXColor = "#B233334C";
			public const string VoiceOverColor = "#CC99004C";

			public const DrawedProperty BasicDrawedProperty = DrawedProperty.Volume | DrawedProperty.PlaybackPosition | DrawedProperty.Fade | DrawedProperty.ClipPreview;

			public const DrawedProperty MusicDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty UIDrawedProperties = BasicDrawedProperty;
			public const DrawedProperty AmbienceDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty SFXDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty VoiceOverDrawedProperties = BasicDrawedProperty;

			public const bool ShowVUColorOnVolumeSlider = true;
		}
	}
}