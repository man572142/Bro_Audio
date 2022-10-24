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
    public class MusicPlayer : AudioPlayer
    {
        private MusicLibrary _currentMusicLibrary;
        private Coroutine _currentPlayCoroutine;
        private Coroutine _stopControlCoroutine;

        public Music CurrentMusic { get => _currentMusicLibrary.Music; }
        public override bool IsPlaying { get; protected set; }
        public override bool IsStoping { get; protected set; }
        public override bool IsFadingOut { get; protected set; }
        public override bool IsFadingIn { get; protected set; }

        private void Start()
        {
            SetClipVolume(0f);
        }

        public void Play(MusicLibrary musicLibrary, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
        {
            _currentPlayCoroutine = StartCoroutine(PlayControl(musicLibrary, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
        }

        private IEnumerator PlayControl(MusicLibrary musicLibrary, float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
        {
            _currentMusicLibrary = musicLibrary;
            AudioSource.clip = musicLibrary.Clip;
            AudioSource.time = musicLibrary.StartPosition;
            AudioSource.loop = musicLibrary.Loop;
            AudioSource.Play();
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
                    SetClipVolume(musicLibrary.Volume);
                }
                #endregion

                #region FadeOut
                fadeOutTime = fadeOutTime < 0 ? musicLibrary.FadeOut : fadeOutTime;
                if (fadeOutTime > 0)
                {
                    yield return new WaitUntil(() => (AudioSource.clip.length - AudioSource.time) <= fadeOutTime);
                    IsFadingOut = true;
                    yield return StartCoroutine(Fade(fadeOutTime, 0f));
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => AudioSource.clip.length == AudioSource.time);
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
                    AudioSource.loop = false;
                    yield return new WaitUntil(() => AudioSource.clip.length == AudioSource.time);
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


        

        private void EndPlaying()
        {
            _currentMusicLibrary = default;
            _currentPlayCoroutine.Stop(this);
            SetClipVolume(0f);    
            AudioSource.Stop();
            AudioSource.clip = null;
            AudioSource.loop = false;
            IsPlaying = false;
            IsStoping = false;
        }

    }


}