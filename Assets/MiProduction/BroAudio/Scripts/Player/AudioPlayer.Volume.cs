using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using System.Collections;
using System;
using System.Collections.Generic;
using static MiProduction.Extension.AnimationExtension;

namespace MiProduction.BroAudio.Runtime
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
        /// ����Clip�����q(0~1)�A�̤��P��Clip�����P�]�w�A��FadeIn/FadeOut�]�u�@�Φb����
        /// </summary>
        public float ClipVolume
        {
            get => _clipVolume;
            private set
            {
                _clipVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        /// <summary>
        /// ���y�����q(0~1)�A�]�i��O��Player�����q�A�@�ά۷��V����Fader
        /// </summary>
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
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
                    if (AudioMixer.GetFloat(VolumeParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel();
                AudioMixer.SetFloat(VolumeParaName, _mixerDecibelVolume);
            }
        }

        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime)
        {
            // �u��TrackVolume
            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            _trackVolumeControlCoroutine = StartCoroutine(Fade(vol, fadeTime, VolumeControl.Track));
            return this;
        }

        private IEnumerator Fade(float targetVol, float duration, VolumeControl fader)
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

            float startVol = GetVol();
            Ease ease = startVol < targetVol ? SoundManager.FadeInEase : SoundManager.FadeOutEase;

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
    }
}
