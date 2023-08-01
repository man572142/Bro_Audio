using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

			Utility.ForeachAudioType((audioType) => 
			{
				if(audioType != BroAudioType.None)
				{
					SetVolume(audioType, GetSliderValue(audioType));
				}
			});
		}

		public void SetVolume(BroAudioType audioType, float value)
		{
			BroAudio.SetVolume(value, audioType);
		}

		private float GetSliderValue(BroAudioType audioType)
		{
			return audioType switch
			{
				BroAudioType.Music => _musicVol.value,
				BroAudioType.UI => _uiVol.value,
				BroAudioType.Ambience => _ambVol.value,
				BroAudioType.SFX => _sfxVol.value,
				BroAudioType.VoiceOver => _voiceVol.value,
				BroAudioType.All => _masterVol.value,
				_ => throw new System.NotImplementedException(),
			};
		}
	}

}