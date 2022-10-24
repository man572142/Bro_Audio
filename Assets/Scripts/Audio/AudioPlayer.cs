using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.AudioExtension;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayer : MonoBehaviour
	{
        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;
        private string _volParaName = string.Empty;

        private Coroutine _subVolumeControl;

        // ClipVolume : ����Clip�����q(0~1)�A�̤��P��Clip�����P�]�w�A��FadeIn/FadeOut�]�u�@�Φb����
        // TrackVolume : ���y�����q(0~1)�A�]�i��O��Player�����q�A�@�ά۷��V����Fader
        // MixerDecibelVolume ��ڦbAudioMixer�W��������

        private float _clipVolume = 1f;
        private float _trackVolume = 1f;
        private float _mixerDecibelVolume = -1;
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume < 0)
                {
                    if (AudioMixer.GetFloat(_volParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                    else
                    {
                        Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel();
                AudioMixer.SetFloat(_volParaName, _mixerDecibelVolume);
            }
        }

        public abstract bool IsPlaying { get; protected set; }
        public abstract bool IsStoping { get; protected set; }
        public abstract bool IsFadingOut { get; protected set; }
        public abstract bool IsFadingIn { get; protected set; }

        protected virtual void Awake()
        {
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
            }
            if (AudioMixer == null)
            {
                AudioMixer = AudioSource.outputAudioMixerGroup.audioMixer;
            }
            _volParaName = AudioSource.outputAudioMixerGroup.name;
        }

        

        public void SetVolume(float vol, float fadeTime)
        {
            // �u��SubVolume�A�ϭ쥻�����q�H��FadeIn/Out�H�Φ��B���q��@�P�B�@
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(TrackVolumeControl(vol, fadeTime));
        }

        protected void SetClipVolume(float vol)
		{
            _clipVolume = vol;
            MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
		}

        protected IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = _clipVolume;
            float targetValue = Mathf.Clamp(targetVolume, MinVolume, MaxVolume);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
                SetClipVolume(newVol);
                yield return null;
            }
            yield break;
        }

        private IEnumerator TrackVolumeControl(float target, float fadeTime)
        {
            float start = _trackVolume;
            float t = 0f;
            while (t < 1f)
            {
                _trackVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                if (!IsFadingIn && !IsFadingOut)
                {
                    Debug.Log(_clipVolume * _trackVolume);
                    MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
                }
                yield return null;
            }
            _trackVolume = target;
            MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
        }
    } 
}
