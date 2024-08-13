using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        /// <summary>
        /// Pitch value without any fading process
        /// </summary>
        public float StaticPitch { get; private set; } = AudioConstant.DefaultPitch;

        private Coroutine _pitchCoroutine;

        IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime)
		{
            StaticPitch = pitch;
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

		private void SetInitialPitch(IAudioEntity entity, IAudioPlaybackPref audioTypePlaybackPref)
		{
			float pitch;
			if(StaticPitch != AudioConstant.DefaultPitch)
			{
				pitch = entity.GetRandomValue(StaticPitch, RandomFlag.Pitch);
			}
			else if(audioTypePlaybackPref.Pitch != AudioConstant.DefaultPitch)
			{
				pitch = entity.GetRandomValue(audioTypePlaybackPref.Pitch, RandomFlag.Pitch);
			}
			else
			{
				pitch = entity.GetPitch();
            }
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

		private void ResetPitch()
		{
            StaticPitch = AudioConstant.DefaultPitch;
			AudioSource.pitch = AudioConstant.DefaultPitch;
        }
	} 
}