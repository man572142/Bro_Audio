using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;

namespace MiProduction.BroAudio
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource _player = null;
        [SerializeField] AudioMixer _audioMixer;
        string _volParaName = string.Empty;
        MusicLibrary _currentMusic;
        Coroutine _currentPlayCoroutine;

        public bool IsPlaying { get; private set; }
        public bool IsFadingOut { get; private set; }

        private void Awake()
        {
            if (_player == null)
            {
                _player = GetComponent<AudioSource>();
            }
            if (_audioMixer == null)
            {
                _audioMixer = _player.outputAudioMixerGroup.audioMixer;
            }
            _volParaName = _player.outputAudioMixerGroup.name;
        }

        private void Start()
        {
            if (!_audioMixer.GetFloat(_volParaName, out float currentVol))
            {
                Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
            }
            _audioMixer.SetFloat(_volParaName, AudioExtension.MinDecibelVolume);
        }


        public void Play(MusicLibrary musicLibrary, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
        {
            _currentPlayCoroutine = StartCoroutine(PlayControl(musicLibrary, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
        }

        private IEnumerator PlayControl(MusicLibrary musicLibrary, float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
        {
            _currentMusic = musicLibrary;
            _player.clip = musicLibrary.clip;
            _player.time = musicLibrary.startPosition;
            _player.loop = musicLibrary.loop;
            _player.Play();
            IsPlaying = true;

            do
            {
                #region FadeIn
                fadeInTime = fadeInTime < 0 ? musicLibrary.fadeIn : fadeInTime;
                if(fadeInTime > 0)
                {
                    yield return StartCoroutine(Fade(fadeInTime, musicLibrary.volume));
                    onFinishFadeIn?.Invoke();
                }    
                else
                {
                    _audioMixer.SetFloat(_volParaName,musicLibrary.volume.ToDecibel());
                }
                #endregion

                #region FadeOut
                fadeOutTime = fadeOutTime < 0 ? musicLibrary.fadeOut : fadeOutTime;
                if (fadeOutTime > 0)
                {
                    yield return new WaitUntil(() => (_player.clip.length - _player.time) <= fadeOutTime);
                    IsFadingOut = true;
                    yield return StartCoroutine(Fade(fadeOutTime, 0f));
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => _player.clip.length == _player.time);
                }
                #endregion
            } while (musicLibrary.loop);

            EndPlaying();
            onFinishPlaying?.Invoke();
        }

        public void Stop(float fadeOutTime = -1, Action onFinishPlaying = null)
        {
            StartCoroutine(StopControl(fadeOutTime, onFinishPlaying));
        }

        private IEnumerator StopControl(float fadeOutTime, Action onFinishPlaying)
        {
            if (_currentMusic.music == Music.None || !IsPlaying)
            {
                onFinishPlaying?.Invoke();
                yield break;
            }

            fadeOutTime = fadeOutTime < 0 ? _currentMusic.fadeOut : fadeOutTime;
            if (fadeOutTime > 0)
            {
                if (IsFadingOut)
                {
                    // 目前設計成:如果原有的音樂已經在FadeOut了，就等它FadeOut不強制停止
                    _player.loop = false;
                    yield return new WaitUntil(() => _player.clip.length == _player.time);
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

        public IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol;
            _audioMixer.GetFloat(_volParaName, out currentVol);
            currentVol = Mathf.Pow(10, currentVol / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
                _audioMixer.SetFloat(_volParaName, Mathf.Log10(newVol) * 20);
                yield return null;
            }
            yield break;
        }

        private void EndPlaying()
        {
            _currentPlayCoroutine.Stop(this);
            _audioMixer.SetFloat(_volParaName, AudioExtension.MinDecibelVolume);        
            _player.Stop();
            _player.clip = null;
            _player.volume = 1f;
            _player.loop = false;
            IsPlaying = false;
        }


    }


}