using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.Demo
{
	public class Mixer : MonoBehaviour
	{
		IAudioAsset _asset;

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
		}

		public void SetVolume(BroAudioType audioType, float value)
		{
			BroAudio.SetVolume(value, audioType);
		}
	}

}