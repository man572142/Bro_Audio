using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Library;
using static MiProduction.BroAudio.AudioExtension;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource _audioSource = null;
        [SerializeField] AudioMixer _audioMixer;
        private string _volParaName = string.Empty;
        private MusicLibrary _currentMusicLibrary;
        private Coroutine _currentPlayCoroutine;
        private Coroutine _subVolumeControl;
        private Coroutine _stopControlCoroutine;

        private float _subVolume = 1f;

        public float MixerVolume
        {
            get
            {
                return MixerDecibelVolume.ToNormalizeVolume();
            }
            private set
            {
                MixerDecibelVolume = value.ToDecibel();
            }
        }

        private float _mixerDecibelVolune = -1;
        private float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolune < 0 && _audioMixer.GetFloat(_volParaName, out float currentVol))
				{
                    _mixerDecibelVolune = currentVol;
				}
                else
				{
                    Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
                }
                return _mixerDecibelVolune;
            }
            set
            {
                float result = (value.ToNormalizeVolume() * SubVolume).ToDecibel();
                _mixerDecibelVolune = Mathf.Clamp(result, MinDecibelVolume, MaxDecibelVolume);
                _audioMixer.SetFloat(_volParaName, _mixerDecibelVolune);
            }
        }

        private float SubVolume
		{
            get
			{
                return _subVolume;
			}
            set
			{
                _subVolume = value;
                // 強迫更新
                MixerDecibelVolume = MixerDecibelVolume;
			}
		}

        public Music CurrentMusic { get => _currentMusicLibrary.Music; }

        public bool IsPlaying { get; private set; }
        public bool IsStoping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }

        private void Awake()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
            if (_audioMixer == null)
            {
                _audioMixer = _audioSource.outputAudioMixerGroup.audioMixer;
            }
            _volParaName = _audioSource.outputAudioMixerGroup.name;
        }

        private void Start()
        {
            MixerVolume = 0f;
        }


        public void Play(MusicLibrary musicLibrary, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
        {
            _currentPlayCoroutine = StartCoroutine(PlayControl(musicLibrary, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
        }

        private IEnumerator PlayControl(MusicLibrary musicLibrary, float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
        {
            _currentMusicLibrary = musicLibrary;
            _audioSource.clip = musicLibrary.Clip;
            _audioSource.time = musicLibrary.StartPosition;
            _audioSource.loop = musicLibrary.Loop;
            _audioSource.Play();
            IsPlaying = true;

            do
            {
                #region FadeIn
                fadeInTime = fadeInTime < 0 ? musicLibrary.FadeIn : fadeInTime;
                if(fadeInTime > 0)
                {
                    IsFadingIn = true;
                    yield return StartCoroutine(Fade(fadeInTime, musicLibrary.Volume));
                    onFinishFadeIn?.Invoke();
                    IsFadingIn = false;
                }    
                else
                {
                    MixerVolume = musicLibrary.Volume;
                }
                #endregion

                #region FadeOut
                fadeOutTime = fadeOutTime < 0 ? musicLibrary.FadeOut : fadeOutTime;
                if (fadeOutTime > 0)
                {
                    yield return new WaitUntil(() => (_audioSource.clip.length - _audioSource.time) <= fadeOutTime);
                    IsFadingOut = true;
                    yield return StartCoroutine(Fade(fadeOutTime, 0f));
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => _audioSource.clip.length == _audioSource.time);
                }
                #endregion
            } while (musicLibrary.Loop);

            EndPlaying();
            onFinishPlaying?.Invoke();
        }

        public void Stop(float fadeOutTime = -1, Action onFinishPlaying = null)
        {
            if(IsStoping)
            {
                Debug.LogWarning("[SoundSystem]The music player is already processing StopMusic !");
                return;
            }
            _stopControlCoroutine = StartCoroutine(StopControl(fadeOutTime, onFinishPlaying));
        }

        private IEnumerator StopControl(float fadeOutTime, Action onFinishPlaying)
        {
            if (_currentMusicLibrary.Music == Music.None || !IsPlaying)
            {
                onFinishPlaying?.Invoke();
                yield break;
            }

            IsStoping = true;
            fadeOutTime = fadeOutTime < 0 ? _currentMusicLibrary.FadeOut : fadeOutTime;
            if (fadeOutTime > 0)
            {
                if (IsFadingOut)
                {
                    // 目前設計成:如果原有的音樂已經在FadeOut了，就等它FadeOut不強制停止
                    _audioSource.loop = false;
                    yield return new WaitUntil(() => _audioSource.clip.length == _audioSource.time);
                }
                else
                {
                    _currentPlayCoroutine.Stop(this);
                    yield return StartCoroutine(Fade(fadeOutTime, 0f));
                }
            }
            EndPlaying();
            onFinishPlaying?.Invoke();
        }


        private IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = Mathf.Pow(10, MixerDecibelVolume / 20);
            float targetValue = Mathf.Clamp(targetVolume, MinVolume, 1);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue , (currentTime / duration).SetEase(ease));
                MixerDecibelVolume = Mathf.Log10(newVol) * 20 ;
                yield return null;
            }
            yield break;
        }

        private void EndPlaying()
        {
            _currentMusicLibrary = default;
            _currentPlayCoroutine.Stop(this);
            MixerVolume = 0f;     
            _audioSource.Stop();
            _audioSource.clip = null;
            _audioSource.volume = 1f;
            _audioSource.loop = false;
            IsPlaying = false;
            IsStoping = false;
        }

        public void SetMusicVolume(float vol,float fadeTime)
		{
            // 只動SubVolume，使原本的音量以及FadeIn/Out以及此處音量能共同運作
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(SubVolumeControl(vol, fadeTime));
        }

        private IEnumerator SubVolumeControl(float target,float fadeTime)
		{
            float currentVol = MixerDecibelVolume;
            float start = SubVolume;
            float t = 0f;
            while(t < 1f)
			{
                //Debug.Log(_subVolume);
                SubVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                if (!IsFadingIn && !IsFadingOut)
                {
                    MixerDecibelVolume = currentVol;
                    MixerDecibelVolume = MixerDecibelVolume;
                }
                yield return null;
			}
            SubVolume = target;
		}

    }


}