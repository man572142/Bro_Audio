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

		public float HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
		public PitchShiftingSetting PitchShifting = PitchShiftingSetting.AudioMixer;

#if UNITY_EDITOR
		public void ResetToFactorySettings()
		{
			HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
			PitchShifting = FactorySettings.PitchShifting;
        }

		public class FactorySettings
		{
			public const float HaasEffectInSeconds = 0.04f;
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