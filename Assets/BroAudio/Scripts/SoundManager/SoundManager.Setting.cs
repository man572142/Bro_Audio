using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public partial class SoundManager : MonoBehaviour
	{
		private RuntimeSetting _setting = null;
		public RuntimeSetting Setting
		{
			get
			{
				if(_setting == null)
					_setting = Resources.Load<RuntimeSetting>(RuntimeSetting.FilePath);

				if(!_setting)
				{
					_setting = new RuntimeSetting();
					LogWarning("Can't load BroAudioGlobalSetting.asset, all setting values will be as default. " +
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