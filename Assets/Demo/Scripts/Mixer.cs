using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Demo
{
	public class Mixer : MonoBehaviour
	{
		[SerializeField] Slider _masterVol = null;
		[SerializeField] Slider _uiVol = null;
		[SerializeField] Slider _musicVol = null;
		[SerializeField] Slider _sfxVol = null;
		[SerializeField] Slider _voiceVol = null;
		[SerializeField] Slider _ambVol = null;

		private void Start()
		{
			_masterVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.All, value));
			_uiVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.UI, value));
			_musicVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.Music, value));
			_sfxVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.SFX, value));
			_voiceVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.VoiceOver, value));
			_ambVol.onValueChanged.AddListener((value) => SetVolume(BroAudioType.Ambience, value));

			Utility.ForeachConcreteAudioType((audioType) => SetVolume(audioType, GetSliderValue(audioType)));
			//Set master
			SetVolume(BroAudioType.All, GetSliderValue(BroAudioType.All));
		}

		public void SetVolume(BroAudioType audioType, float value)
		{
			BroAudio.SetVolume(value, audioType,BroAdvice.FadeTime_Immediate);
		}

		private float GetSliderValue(BroAudioType audioType)
		{
			switch (audioType)
			{
				case BroAudioType.Music:
					return _musicVol.value;
				case BroAudioType.UI:
					return _uiVol.value;
				case BroAudioType.Ambience:
					return _ambVol.value;
				case BroAudioType.SFX:
					return _sfxVol.value;
				case BroAudioType.VoiceOver:
					return _voiceVol.value;
				case BroAudioType.All:
					return _masterVol.value;
				default:
					BroLog.LogError($"Can't get value from {audioType}");
					return -1f;
			}
		}
	}

}