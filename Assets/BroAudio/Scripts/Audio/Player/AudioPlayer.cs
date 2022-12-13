using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.AudioExtension;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayer : MonoBehaviour
	{
        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;
        private string _volParaName = string.Empty;

        private Coroutine _subVolumeControl;

        // ClipVolume : 播放Clip的音量(0~1)，依不同的Clip有不同設定，而FadeIn/FadeOut也只作用在此值
        // TrackVolume : 音軌的音量(0~1)，也可算是此Player的音量，作用相當於混音的Fader
        // MixerDecibelVolume 實際在AudioMixer上的分貝數

        private float _clipVolume = 1f;
        public float ClipVolume
        {
            get => _clipVolume;
            protected set
            {
                _clipVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        private float _trackVolume = 1f;
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }
        
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
                        LogError("Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
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
            // 只動TrackVolume
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(TrackVolumeControl(vol, fadeTime));
        }

        protected IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = ClipVolume;
            float targetValue = Mathf.Clamp(targetVolume, MinVolume, MaxVolume);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
                ClipVolume = newVol;
                yield return null;
            }
            yield break;
        }

        private IEnumerator TrackVolumeControl(float target, float fadeTime)
        {
            float start = TrackVolume;
            float t = 0f;
            while (t < 1f)
            {
                TrackVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                yield return null;
            }
            TrackVolume = target;
        }
    } 
}
