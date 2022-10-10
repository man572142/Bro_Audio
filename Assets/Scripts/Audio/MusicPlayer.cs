using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Library;
using static UnityEngine.GraphicsBuffer;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] AudioSource _player = null;
        [SerializeField] AudioMixer _audioMixer;
        private string _volParaName = string.Empty;
        private MusicLibrary _currentMusic;
        private Coroutine _currentPlayCoroutine;
        private Coroutine _subVolumeControl;
        private Coroutine _stopControlCoroutine;

        private float _subVolume = 1f;

        public float MixerVolume
        {
            get
            {
                return (MixerDecibelVolume / AudioExtension.MinDecibelVolume * -1f) + 1f;
            }
            private set
            {
                MixerDecibelVolume = value.ToDecibel();
            }
        }

        float MixerDecibelVolume
        {
            get
            {
                if (!_audioMixer.GetFloat(_volParaName, out float currentVol))
                {
                    Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
                }
                return currentVol;
            }
            set
            {
                _audioMixer.SetFloat(_volParaName, value * _subVolume);
            }
        }

        public Music CurrentMusic { get => _currentMusic.Music; }

        public bool IsPlaying { get; private set; }
        public bool IsStoping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }

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
            MixerVolume = 0f;
        }


        public void Play(MusicLibrary musicLibrary, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
        {
            _currentPlayCoroutine = StartCoroutine(PlayControl(musicLibrary, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
        }

        private IEnumerator PlayControl(MusicLibrary musicLibrary, float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
        {
            Debug.Log("PlayMusicCoroutine");
            _currentMusic = musicLibrary;
            _player.clip = musicLibrary.Clip;
            _player.time = musicLibrary.StartPosition;
            _player.loop = musicLibrary.Loop;
            _player.Play();
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
            } while (musicLibrary.Loop);

            EndPlaying();
            onFinishPlaying?.Invoke();
        }

        public void Stop(float fadeOutTime = -1, Action onFinishPlaying = null)
        {
            if(IsStoping)
            {
                Debug.Log("WTF");
                return;
            }
            _stopControlCoroutine = StartCoroutine(StopControl(fadeOutTime, onFinishPlaying));
        }

        private IEnumerator StopControl(float fadeOutTime, Action onFinishPlaying)
        {
            Debug.Log("StopMusicCoroutine");

            if (_currentMusic.Music == Music.None || !IsPlaying)
            {
                onFinishPlaying?.Invoke();
                yield break;
            }

            fadeOutTime = fadeOutTime < 0 ? _currentMusic.FadeOut : fadeOutTime;
            if (fadeOutTime > 0)
            {
                if (IsFadingOut)
                {
                    Debug.Log("ISFADEOUT");
                    // �ثe�]�p��:�p�G�즳�����֤w�g�bFadeOut�F�A�N����FadeOut���j���
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


        private IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = Mathf.Pow(10, MixerDecibelVolume / 20);
            float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue , (currentTime / duration).SetEase(ease));
                MixerDecibelVolume = Mathf.Log10(newVol) * 20;
                yield return null;
            }
            yield break;
        }

        private void EndPlaying()
        {
            Debug.Log("End");
            _currentMusic = default;
            _currentPlayCoroutine.Stop(this);
            MixerVolume = 0f;     
            _player.Stop();
            _player.clip = null;
            _player.volume = 1f;
            _player.loop = false;
            IsPlaying = false;
        }

        public void FadeSubVolume(float vol,float fadeTime = 1f)
		{
            // �u��SubVolume�A�ϭ쥻�����q�H��FadeIn/Out�H�Φ��B���q��@�P�B�@
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(SubVolumeControl(vol, fadeTime));
        }

        private IEnumerator SubVolumeControl(float target,float fadeTime)
		{
            float start = _subVolume;
            float t = 0f;
            while(t < 1f)
			{
                _subVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                if (!IsFadingIn && !IsFadingOut)
                {
                    // ���{�b�� �A���Wsub  �٨S�g��!!
                    MixerVolume = target;
                }
                yield return null;
			}
		}

    }


}