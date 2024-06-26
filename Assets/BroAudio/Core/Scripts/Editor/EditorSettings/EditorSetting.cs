using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = nameof(BroAudio) + "/Editor Setting", fileName = Tools.BroName.EditorSettingPath)]
#endif
	public class EditorSetting : ScriptableObject
	{
		[System.Serializable]
		public struct AudioTypeSetting
		{
            // AudioEntityPropertyDrawer rely on the immutability, don't chagne to class without modifying it
            public BroAudioType AudioType;
			public Color Color;
			public DrawedProperty DrawedProperty;

			public AudioTypeSetting(BroAudioType audioType,string colorString, DrawedProperty drawedProperty)
			{
				AudioType = audioType;
				ColorUtility.TryParseHtmlString(colorString, out Color);
				DrawedProperty = drawedProperty;
			}

			public bool CanDraw(DrawedProperty target) => DrawedProperty.Contains(target);
		}

		public bool ShowAudioTypeOnSoundID;
		public bool ShowVUColorOnVolumeSlider;
		public bool ShowMasterVolumeOnClipListHeader;

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
			ShowMasterVolumeOnClipListHeader = FactorySettings.ShowMasterVolumeOnClipListHeader;
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
			public const bool ShowVUColorOnVolumeSlider = true;
			public const bool ShowMasterVolumeOnClipListHeader = false;

			public const string MusicColor = "#012F874C";
			public const string UIColor = "#0E9C884C";
			public const string AmbienceColor = "#00B0284C";
			public const string SFXColor = "#FD803D96";
			public const string VoiceOverColor = "#EEC6374C";

			public const DrawedProperty BasicDrawedProperty = DrawedProperty.Volume | DrawedProperty.PlaybackPosition | DrawedProperty.Fade | DrawedProperty.ClipPreview | DrawedProperty.MasterVolume;

			public const DrawedProperty MusicDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop;
			public const DrawedProperty UIDrawedProperties = BasicDrawedProperty;
			public const DrawedProperty AmbienceDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop | DrawedProperty.SpatialSettings;
			public const DrawedProperty SFXDrawedProperties = BasicDrawedProperty | DrawedProperty.Loop | DrawedProperty.SpatialSettings | DrawedProperty.Pitch;
			public const DrawedProperty VoiceOverDrawedProperties = BasicDrawedProperty;
		}
	}
}