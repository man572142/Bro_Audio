using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Data
{
	[CreateAssetMenu(menuName = "BroAudio(DevOnly)/Create Global Setting Asset File",fileName ="BroAudioGlobalSetting")]
	public class GlobalSetting : ScriptableObject
	{
		public float HaasEffectInSeconds = 0.04f;
		public Ease DefaultFadeInEase = Ease.InCubic;
		public Ease DefaultFadeOutEase = Ease.OutSine;
		public Ease SeamlessFadeInEase = Ease.OutCubic;
		public Ease SeamlessFadeOutEase = Ease.OutSine;

		public int DefaultAudioPlayerPoolSize = 5;

#if UNITY_EDITOR
		public bool ShowAudioTypeOnAudioID = true;
		public bool ShowVUColorOnVolumeSlider = true;
#endif
	}

}