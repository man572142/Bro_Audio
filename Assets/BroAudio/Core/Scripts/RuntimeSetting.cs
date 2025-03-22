using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Runtime;
using System;

namespace Ami.BroAudio.Data
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = nameof(BroAudio) + "/Runtime Setting", fileName = Tools.BroName.RuntimeSettingName)]
#endif
	public class RuntimeSetting : ScriptableObject
	{
        [Obsolete("This feature has been moved to " + nameof(PlaybackGroup)), HideInInspector]
		public float CombFilteringPreventionInSeconds = FactorySettings.CombFilteringPreventionInSeconds;
		public bool LogAccessRecycledPlayerWarning = true;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
		public FilterSlope AudioFilterSlope = FactorySettings.AudioFilterSlope;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
		public PitchShiftingSetting PitchSetting = FactorySettings.PitchShifting;

		public bool AlwaysPlayMusicAsBGM = true;
		public Transition DefaultBGMTransition = FactorySettings.DefaultBGMTransition;
		public float DefaultBGMTransitionTime = FactorySettings.DefaultBGMTransitionTime;

        public PlaybackGroup GlobalPlaybackGroup = null;

#if UNITY_EDITOR
		public void ResetToFactorySettings()
		{
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			AudioFilterSlope = FactorySettings.AudioFilterSlope;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
			PitchSetting = FactorySettings.PitchShifting;
			AlwaysPlayMusicAsBGM = true;
			DefaultBGMTransition = FactorySettings.DefaultBGMTransition;
			DefaultBGMTransitionTime = FactorySettings.DefaultBGMTransitionTime;
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
			public const float DefaultBGMTransitionTime = 2f;

        }
	}
}