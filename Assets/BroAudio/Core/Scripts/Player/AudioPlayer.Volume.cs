using UnityEngine;
using Ami.Extension;
using System.Collections;
using System;
using System.Collections.Generic;
using static Ami.Extension.AnimationExtension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
	{
        private enum VolumeControl
        {
            Clip,
            Track,
            MixerDecibel,
        }

        public const float DefaultClipVolume = 0f;
        public const float DefaultTrackVolume = AudioConstant.FullVolume;
        public const float DefaultMixerDecibelVolume = int.MinValue;

        private float _clipVolume = 0f;
        private float _trackVolume = DefaultTrackVolume;
        private float _mixerDecibelVolume = DefaultMixerDecibelVolume;

        /// <summary>
        /// The playback volume of an audio clip, it's determined by the clip's property settings, such as Fade In/Out.   
        /// </summary>
        public float ClipVolume
        {
            get => _clipVolume;
            private set
            {
                _clipVolume = value;
#if UNITY_WEBGL
                WebGLSetVolume();
#else
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
#endif
            }
        }

        /// <summary>
        /// The playback volume of an audio track, it will be set by SetVolume() function.
        /// </summary>
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
#if UNITY_WEBGL
                WebGLSetVolume();
#else
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
#endif
            }
        }

        /// <summary>
        /// Track volume without any fading process
        /// </summary>
        public float StaticTrackVolume { get; private set; } = DefaultTrackVolume;

        /// <summary>
        /// The final decibel volume that is set in the mixer.
        /// </summary>
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume == DefaultMixerDecibelVolume)
                {
                    if (_audioMixer.SafeGetFloat(VolumeParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel(true);
				_audioMixer.SafeSetFloat(VolumeParaName, _mixerDecibelVolume);
			}
        }

        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime)
        {
            StaticTrackVolume = vol; // in case the fading process is interrupted
            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            if(fadeTime > 0)
			{
                Ease ease = TrackVolume < vol ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
                _trackVolumeControlCoroutine = StartCoroutine(Fade(vol, fadeTime, VolumeControl.Track, ease));
            }
            else
			{
                TrackVolume = vol;
			}

            return this;
        }

        private IEnumerator Fade(float targetVol, float duration, VolumeControl fader,Ease ease)
        {
            Func<float> GetVol = null;
            Action<float> SetVol = null;

            switch (fader)
            {
                case VolumeControl.Clip:
                    GetVol = () => ClipVolume;
                    SetVol = (vol) => ClipVolume = vol;
                    break;
                case VolumeControl.Track:
                    GetVol = () => TrackVolume;
                    SetVol = (vol) => TrackVolume = vol;
                    break;
                case VolumeControl.MixerDecibel:
                    break;
            }

            if(duration <= 0)
			{
                SetVol(targetVol);
                yield break;
			}

            float startVol = GetVol();

            IEnumerable<float> volumes = GetLerpValuesPerFrame(startVol, targetVol, duration, ease);
            if (volumes != null)
            {
                foreach (float vol in volumes)
                {
                    SetVol(vol);
                    yield return null;
                }
            }
        }

        private void ResetVolume()
        {
            _clipVolume = DefaultClipVolume;
            _trackVolume = DefaultTrackVolume;
            _mixerDecibelVolume = DefaultMixerDecibelVolume;
            StaticTrackVolume = DefaultTrackVolume;
        }

#if UNITY_WEBGL
        private void WebGLSetVolume()
        {
            AudioSource.volume = AudioExtension.ClampNormalize(_clipVolume * _trackVolume);
        }
#endif
    }
}
