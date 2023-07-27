using System.Collections;
using System.Collections.Generic;
using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
	public partial class SoundManager : MonoBehaviour
	{
		private GlobalSetting _setting = null;
		public GlobalSetting Setting
		{
			get
			{
				_setting ??= Resources.Load<GlobalSetting>(GlobalSetting.FileName);
				if(!_setting)
				{
					_setting = new GlobalSetting();
					Utility.LogWarning("Can't load BroAudioGlobalSetting.asset, all setting values will be as default. " +
						"If your setting file is missing. Please open BroAudio/Setting to recreate it and put it under any [Resource] folder");
				}
				return _setting;
			}
		}

		public static Ease FadeInEase => Instance.Setting.DefaultFadeInEase;
		public static Ease FadeOutEase => Instance.Setting.DefaultFadeOutEase;
		public static Ease SeamlessFadeIn => Instance.Setting.SeamlessFadeInEase;
		public static Ease SeamlessFadeOut => Instance.Setting.SeamlessFadeOutEase;

		public static float HaasEffectInSeconds => Instance.Setting.HaasEffectInSeconds;
	}
}