using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
	//[CreateAssetMenu(menuName = "BroAudio(DevOnly)/Create Runtime Setting Asset File", fileName = "BroAudioRuntimeSetting")]
	public class RuntimeSetting : ScriptableObject
	{
		public const string FilePath = "BroAudioGlobalSetting";

		public float HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
		public Ease DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
		public Ease DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
		public Ease SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
		public Ease SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;

		public int DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;

#if UNITY_EDITOR
		public void ResetToFactorySettings()
		{
			HaasEffectInSeconds = FactorySettings.HaasEffectInSeconds;
			DefaultFadeInEase = FactorySettings.DefaultFadeInEase;
			DefaultFadeOutEase = FactorySettings.DefaultFadeOutEase;
			SeamlessFadeInEase = FactorySettings.SeamlessFadeInEase;
			SeamlessFadeOutEase = FactorySettings.SeamlessFadeOutEase;
			DefaultAudioPlayerPoolSize = FactorySettings.DefaultAudioPlayerPoolSize;
        }

		public class FactorySettings
		{
			public const float HaasEffectInSeconds = 0.04f;
			public const Ease DefaultFadeInEase = Ease.InCubic;
			public const Ease DefaultFadeOutEase = Ease.OutSine;
			public const Ease SeamlessFadeInEase = Ease.OutCubic;
			public const Ease SeamlessFadeOutEase = Ease.OutSine;

			public const int DefaultAudioPlayerPoolSize = 5;
		}
#endif
	}

}