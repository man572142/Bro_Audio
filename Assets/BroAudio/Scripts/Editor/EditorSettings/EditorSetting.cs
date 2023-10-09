using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = "BroAudio/Create Editor Setting Asset File", fileName = FileName)]
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

		public const string FileName = "BroEditorSetting";
		public const string FilePath = "Editor/" + FileName;

		public bool ShowAudioTypeOnAudioID;
		public bool ShowVUColorOnVolumeSlider;

		public List<AudioTypeSetting> AudioTypeSettings;

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
					Color = Color.red,
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
			ShowAudioTypeOnAudioID = FactorySettings.ShowAudioTypeOnAudioID;
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
			public const bool ShowAudioTypeOnAudioID = true;

			public const string MusicColor = "#001A4C4C";
			public const string UIColor = "#0080334C";
			public const string AmbienceColor = "#1A80804C";
			public const string SFXColor = "#B233334C";
			public const string VoiceOverColor = "#CC99004C";

			public const DrawedProperty BasicDrawedProperty = DrawedProperty.Volume | DrawedProperty.PlaybackPosition | DrawedProperty.Fade | DrawedProperty.ClipPreview;

			public const DrawedProperty MusicDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty UIDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay;
			public const DrawedProperty AmbienceDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty SFXDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay | DrawedProperty.Loop;
			public const DrawedProperty VoiceOverDrawedProperties = BasicDrawedProperty | DrawedProperty.Delay;

			public const bool ShowVUColorOnVolumeSlider = true;
		}
	}
}