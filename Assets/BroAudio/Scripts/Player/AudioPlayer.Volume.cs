using UnityEngine;
using Ami.Extension;
using System.Collections;
using System;
using System.Collections.Generic;
using static Ami.Extension.AnimationExtension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        private enum VolumeControl
        {
            Clip,
            Track,
            MixerDecibel,
        }

        public const float DefaultClipVolume = 0f;
        public const float DefaultTrackVolume = 1f;
        public const float DefaultMixerDecibelVolume = int.MinValue;

        private float _clipVolume = DefaultClipVolume;
        private float _trackVolume = DefaultTrackVolume;
        private float _mixerDecibelVolume = DefaultMixerDecibelVolume;
        
        public float TrackVolumeBeforeMute { get; private set; } = DefaultTrackVolume;

        /// <summary>
        /// ����Clip�����q�A�̤��P��Clip�����P�]�w�A��FadeIn/FadeOut�]�u�@�Φb����
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
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel(true);
#endif
            }
        }

        /// <summary>
        /// ���y�����q�A�]�i��O��Player�����q�A�@�ά۷���V����Fader
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
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel(true);
#endif
            }
        }

        /// <summary>
        /// MixerDecibelVolume ��ڦbAudioMixer�W��������
        /// </summary>
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume == DefaultMixerDecibelVolume)
                {
                    if (_audioMixer.GetFloat(VolumeParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel(true);
                if(!string.IsNullOrEmpty(VolumeParaName))
				{
                    _audioMixer.SetFloat(VolumeParaName, _mixerDecibelVolume);
                }
            }
        }

        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime)
        {
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
            TrackVolumeBeforeMute = DefaultTrackVolume;
        }

#if UNITY_WEBGL
        private void WebGLSetVolume()
        {
            AudioSource.volume = AudioExtension.ClampNormalize(_clipVolume * _trackVolume);
        }
#endif
    }
}
