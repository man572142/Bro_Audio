using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Data
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = "BroAudio/Create Runtime Setting Asset File", fileName = FileName)]
#endif
	public class RuntimeSetting : ScriptableObject
	{
		public const string FileName = "BroRuntimeSetting";
		public const string FilePath = FileName;

		public float CombFilteringPreventionInSeconds = FactorySettings.CombFilteringPreventionInSeconds;
		public bool LogCombFilteringWarning = true;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
		public PitchShiftingSetting PitchSetting = PitchShiftingSetting.AudioMixer;

#if UNITY_EDITOR
		public void ResetToFactorySettings()
		{
			CombFilteringPreventionInSeconds = FactorySettings.CombFilteringPreventionInSeconds;
			LogCombFilteringWarning = true;
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
			PitchSetting = FactorySettings.PitchShifting;
        }

		public class FactorySettings
		{
			public const float CombFilteringPreventionInSeconds = 0.04f;
			public const Ease DefaultFadeInEase = Ease.InCubic;
			public const Ease DefaultFadeOutEase = Ease.OutSine;
			public const Ease SeamlessFadeInEase = Ease.OutCubic;
			public const Ease SeamlessFadeOutEase = Ease.OutSine;

			public const int DefaultAudioPlayerPoolSize = 5;
			public const PitchShiftingSetting PitchShifting = PitchShiftingSetting.AudioMixer;
		}
#endif
	}
}