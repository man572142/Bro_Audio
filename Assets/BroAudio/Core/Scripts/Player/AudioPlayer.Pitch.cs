using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IRecyclable<AudioPlayer>
	{
		private Coroutine _pitchCoroutine;

		IAudioPlayer IPitchSettable.SetPitch(float pitch, float fadeTime)
		{
			switch (SoundManager.PitchSetting)
			{
				case PitchShiftingSetting.AudioMixer:
					//_audioMixer.SafeSetFloat(_pitchParaName, pitch); // Don't * 100f, the value in percentage is displayed in Editor only.  
					break;
				case PitchShiftingSetting.AudioSource:
					pitch = Mathf.Clamp(pitch, AudioConstant.MinAudioSourcePitch, AudioConstant.MaxAudioSourcePitch);
					if (fadeTime > 0f)
					{
						this.StartCoroutineAndReassign(PitchControl(pitch, fadeTime), ref _pitchCoroutine);
                    }
					else
					{
						AudioSource.pitch = pitch;
					}
					break;
			}
			return this;
		}

		private void SetInitialPitch(IAudioEntity entity)
		{
			float pitch = entity.GetPitch();
			AudioSource.pitch = pitch;
		}

		private IEnumerator PitchControl(float targetPitch, float fadeTime)
		{
			var pitchs = AnimationExtension.GetLerpValuesPerFrame(AudioSource.pitch, targetPitch, fadeTime, Ease.Linear);

			foreach (var pitch in pitchs)
			{
				AudioSource.pitch = pitch;
				yield return null;
			}
		}
	} 
}