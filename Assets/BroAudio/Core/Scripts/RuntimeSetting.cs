using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Data
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = nameof(BroAudio) + "/Runtime Setting", fileName = BroName.RuntimeSettingFileName)]
#endif
	public class RuntimeSetting : ScriptableObject
	{
		public float CombFilteringPreventionInSeconds = FactorySettings.CombFilteringPreventionInSeconds;
		public bool LogCombFilteringWarning = false;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
		public FilterSlope AudioFilterSlope = FactorySettings.AudioFilterSlope;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
		public PitchShiftingSetting PitchSetting = FactorySettings.PitchShifting;

		public bool AlwaysPlayMusicAsBGM = true;
		public Transition DefaultBGMTransition = FactorySettings.DefaultBGMTransition;

#if UNITY_EDITOR
		public void ResetToFactorySettings()
		{
			CombFilteringPreventionInSeconds = FactorySettings.CombFilteringPreventionInSeconds;
			LogCombFilteringWarning = false;
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
			PitchSetting = FactorySettings.PitchShifting;
			AlwaysPlayMusicAsBGM = true;
			DefaultBGMTransition = FactorySettings.DefaultBGMTransition;
		}
#endif
        public class FactorySettings
		{
			public const float CombFilteringPreventionInSeconds = 0.04f;
			public const Ease DefaultFadeInEase = Ease.InCubic;
			public const Ease DefaultFadeOutEase = Ease.OutSine;
			public const Ease SeamlessFadeInEase = Ease.OutCubic;
			public const Ease SeamlessFadeOutEase = Ease.OutSine;
			public const FilterSlope AudioFilterSlope = FilterSlope.FourPole;

			public const int DefaultAudioPlayerPoolSize = 5;
			public const PitchShiftingSetting PitchShifting = PitchShiftingSetting.AudioSource;
			public const Transition DefaultBGMTransition = Transition.CrossFade;
		}
	}
}