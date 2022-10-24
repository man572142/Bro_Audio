using MiProduction.BroAudio.Library;
using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.AudioExtension;
using static UnityEngine.Networking.UnityWebRequest;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayer : MonoBehaviour
	{
        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;
        private string _volParaName = string.Empty;

        private Coroutine _subVolumeControl;
        
        private float _mixerDecibelVolume = -1;
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume < 0)
                {
                    if (AudioMixer.GetFloat(_volParaName, out float currentVol))
                    {
                        //Debug.Log(currentVol);
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

        private float _subVolume = 1f;


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
            // 只動SubVolume，使原本的音量以及FadeIn/Out以及此處音量能共同運作
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(SubVolumeControl(vol, fadeTime));
        }

        protected void SetMixerNormalizeVolume(float vol)
		{
            MixerDecibelVolume = (vol * _subVolume).ToDecibel();
		}

        protected IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = MixerDecibelVolume.ToNormalizeVolume();
            float targetValue = Mathf.Clamp(targetVolume, MinVolume, MaxVolume);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
                MixerDecibelVolume = newVol.ToDecibel();
                yield return null;
            }
            yield break;
        }

        private IEnumerator SubVolumeControl(float target, float fadeTime)
        {
            float start = _subVolume;
            float startMixerVol = MixerDecibelVolume.ToNormalizeVolume();
            float t = 0f;
            while (t < 1f)
            {
                //Debug.Log(_subVolume);
                _subVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                if (!IsFadingIn && !IsFadingOut)
                {
                    MixerDecibelVolume = (startMixerVol * _subVolume).ToDecibel();
                }
                yield return null;
            }
            _subVolume = target;
            MixerDecibelVolume = (startMixerVol * _subVolume).ToDecibel();
        }
    } 
}
