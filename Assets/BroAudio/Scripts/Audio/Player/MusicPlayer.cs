using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Library;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Core
{
    public class MusicPlayer : AudioPlayer
    {
        private Coroutine _currentPlayCoroutine;
        private Coroutine _stopControlCoroutine;
        private BroAudioClip _currentClip;

        public int CurrentMusicID { get; private set; } = -1;
        public override bool IsPlaying { get; protected set; }
        public override bool IsStoping { get; protected set; }
        public override bool IsFadingOut { get; protected set; }
        public override bool IsFadingIn { get; protected set; }

        private void Start()
        {
            ClipVolume = 0f;
        }

        public void Play(int id,BroAudioClip clip, bool isLoop, float fadeInTime = -1f, float fadeOutTime = -1f, Action onFinishFadeIn = null, Action onFinishPlaying = null)
        {
            CurrentMusicID = id;
            _currentClip = clip;
            _currentPlayCoroutine = StartCoroutine(PlayControl(clip,isLoop, fadeInTime, fadeOutTime, onFinishFadeIn, onFinishPlaying));
        }

        private IEnumerator PlayControl(BroAudioClip clip,bool isLoop ,float fadeInTime, float fadeOutTime, Action onFinishFadeIn, Action onFinishPlaying)
        {
            AudioSource.clip = clip.AudioClip;  
            AudioSource.time = clip.StartPosition;
            AudioSource.loop = isLoop;
            AudioSource.Play();
            IsPlaying = true;

            do
            {
                #region FadeIn
                fadeInTime = fadeInTime < 0 ? clip.FadeIn : fadeInTime;
                if(fadeInTime > 0)
                {
                    IsFadingIn = true;
                    yield return StartCoroutine(Fade(fadeInTime, clip.Volume));
                    onFinishFadeIn?.Invoke();
                    IsFadingIn = false;
                }    
                else
                {
                    ClipVolume = clip.Volume;
                }
                #endregion

                #region FadeOut
                fadeOutTime = fadeOutTime < 0 ? clip.FadeOut : fadeOutTime;
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
            } while (isLoop);

            EndPlaying();
            onFinishPlaying?.Invoke();
        }

        public override void Stop(float fadeTime)
        {
            Stop(fadeTime);
        }

        public void Stop(float fadeOutTime = -1, Action onFinishPlaying = null)
        {
            if(IsStoping)
            {
                LogWarning("The music player is already processing StopMusic !");
                return;
            }
            _stopControlCoroutine = StartCoroutine(StopControl(fadeOutTime, onFinishPlaying));
        }

        private IEnumerator StopControl(float fadeOutTime, Action onFinishPlaying)
        {
            if (CurrentMusicID <= 0 || !IsPlaying)
            {
                EndPlaying();
                onFinishPlaying?.Invoke();
                yield break;
            }

            IsStoping = true;
            fadeOutTime = fadeOutTime < 0 ? _currentClip.FadeOut : fadeOutTime;
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
            _currentClip.ClearData();
            CurrentMusicID = -1;
            _currentPlayCoroutine.Stop(this);
            ClipVolume = 0f;    
            AudioSource.Stop();
            AudioSource.clip = null;
            AudioSource.loop = false;
            IsPlaying = false;
            IsStoping = false;
        }
	}


}