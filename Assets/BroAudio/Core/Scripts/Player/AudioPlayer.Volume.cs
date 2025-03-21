using UnityEngine;
using Ami.Extension;
using System.Collections;
using System;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        public const float DefaultClipVolume = 0f;
        public const float DefaultTrackVolume = AudioConstant.FullVolume;
        public const float UnSetMixerDecibelVolume = float.MinValue;

        /// <summary>
        /// The playback volume of an audio track, it will be set by SetVolume() function.
        /// </summary>
        private Fader _trackVolume = null;

        /// <summary>
        /// The playback volume of an audio clip, it's determined by the clip's property settings, such as Fade In/Out.   
        /// </summary>
        private Fader _clipVolume = null;

        /// <summary>
        /// The playback volume of the audio type this player belongs to, this is used when being set by SetVolume(BroAudioType)
        /// </summary>
        private Fader _audioTypeVolume = null;

        private float _mixerDecibelVolume = UnSetMixerDecibelVolume;

        /// <summary>
        /// Try get the final decibel volume that is set in the mixer.
        /// </summary>
        private bool TryGetMixerDecibelVolume(out float vol)
        {
            if (_mixerDecibelVolume == UnSetMixerDecibelVolume && TryGetMixerAndTrack(out var mixer, out _) 
                && mixer.SafeGetFloat(VolumeParaName, out float currentVol))
            {
                _mixerDecibelVolume = currentVol;
            }
            vol = _mixerDecibelVolume;
            return vol > UnSetMixerDecibelVolume;
        }

        /// <summary>
        /// Set the final decibel volume that is set in the mixer.
        /// </summary>
        private bool TrySetMixerDecibelVolume(float vol)
        {
            if(TryGetMixerAndTrack(out var mixer, out _))
            {
                _mixerDecibelVolume = vol.ClampDecibel(true);  
                return mixer.SafeSetFloat(VolumeParaName, _mixerDecibelVolume);
            }
            return false;
        }

        private void InitVolumeModule()
        {
            Action updateMixer = UpdateVolume;
            _trackVolume = new Fader(DefaultTrackVolume, updateMixer);
            _clipVolume = new Fader(DefaultClipVolume, updateMixer);
            _audioTypeVolume = new Fader(DefaultTrackVolume, updateMixer);
        }

        private void UpdateVolume()
        {
#if UNITY_WEBGL
            UpdateWebGLVolume();
#else
            float vol = _clipVolume.Current * _trackVolume.Current * _audioTypeVolume.Current;
            if(!TrySetMixerDecibelVolume(vol.ToDecibel()))
            {
                AudioSource.volume = vol.ClampNormalize();
            }
#endif
        }

        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime)
        {
            SetVolumeInternal(_trackVolume, vol, fadeTime);
            return this;
        }

        public void SetAudioTypeVolume(float vol, float fadeTime)
        {
            SetVolumeInternal(_audioTypeVolume, vol, fadeTime);
        }

        private IEnumerator Fade(Fader volume, float fadeTime, Ease ease)
        {
            float elapsedTime = 0f;
            while(volume.Update(ref elapsedTime, fadeTime, ease))
            {
                yield return null;
            }
        }

        private void SetVolumeInternal(Fader module, float vol, float fadeTime)
        {
            module.SetTarget(vol);
            if (fadeTime > 0f)
            {   
                Ease ease = module.Current < vol ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
                module.StartCoroutineAndReassign(Fade(module, fadeTime, ease));
            }
            else
            {
                module.Complete(vol, IsPlaying);
            }
        }

        private void ResetVolume()
        {
            _clipVolume.Complete(DefaultClipVolume, false);
            _trackVolume.Complete(DefaultTrackVolume, false);
            _audioTypeVolume.Complete(DefaultTrackVolume, false);
            UpdateVolume();
        }

#if UNITY_WEBGL
        public void UpdateWebGLVolume()
        {
            float masterVolume = SoundManager.Instance.WebGLMasterVolume;
            AudioSource.volume = AudioExtension.ClampNormalize(_clipVolume.Current * _trackVolume.Current * _audioTypeVolume.Current * masterVolume);
        }
#endif
    }
}
