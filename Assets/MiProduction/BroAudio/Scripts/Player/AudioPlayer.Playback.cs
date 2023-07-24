using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.AnimationExtension;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        public static Dictionary<int, AudioPlayer> ResumablePlayers = null;

        public event Action<int,BroAudioClip,PlaybackPreference> OnFinishingStarted = null;

        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine;
        private Coroutine _trackVolumeControlCoroutine;
        private Coroutine _recycleCoroutine;
        private bool _isReadyToPlay = false;

        public void Play(int id, BroAudioClip clip, PlaybackPreference pref)
        {
            ID = id;
            CurrentClip = clip;
            _isReadyToPlay = true;
            IsStopping = false;
            this.SafeStopCoroutine(_recycleCoroutine);

            AsyncTaskExtension.ExecuteAfterChainingMethod(() => 
            {
                if(_isReadyToPlay)
                {
                    DecoratePlaybackPreference?.Invoke(pref);
                    this.StartCoroutineAndReassign(PlayControl(clip, pref), ref _playbackControlCoroutine);
                }
                else
                {
                    EndPlaying();
                }
            });
        }

        private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference pref)
        {
            if(pref.HaveToWaitForPrevious)
			{
                yield return new WaitUntil(() => pref.HaveToWaitForPrevious == false);
			}

            if (pref.Delay > 0)
			{
                yield return new WaitForSeconds(pref.Delay);
            }

            AudioSource.clip = clip.AudioClip;
            VolumeControl fader = VolumeControl.Clip;
            ClipVolume = 0f;
            RemoveFromResumablePlayer();

            do
            {
                switch (_stopMode)
                {
                    case StopMode.Stop:
                        PlayFromPos(clip.StartPosition);
                        break;
                    case StopMode.Pause:
                        AudioSource.UnPause();
                        break;
                    case StopMode.Mute:
                        this.SetVolume(TrackVolumeBeforeMute);
                        if (!AudioSource.isPlaying)
                        {
                            PlayFromPos(clip.StartPosition);
                        }
                        break;
                }
                _stopMode = default;

                #region FadeIn
                if (HasFading(clip.FadeIn, pref.FadeIn, out float fadeIn))
                {
                    IsFadingIn = true;
                    yield return Fade(clip.Volume, fadeIn, fader,pref.FadeInEase);
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = clip.Volume;
                }
                #endregion

                float endTime = AudioSource.clip.length - clip.EndPosition;
                #region FadeOut
                if (HasFading(clip.FadeOut, pref.FadeOut, out float fadeOut))
                {
                    yield return new WaitUntil(() => (endTime - AudioSource.time) <= fadeOut);
                    IsFadingOut = true;
                    StartToFinish(clip, pref);
                    yield return Fade(0f, fadeOut, fader,pref.FadeOutEase);
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => AudioSource.time >= endTime);
                    StartToFinish(clip, pref);
                }
                #endregion
            } while (pref.IsLoop);

            EndPlaying();

			void PlayFromPos(float pos)
			{
				AudioSource.Stop();
				AudioSource.time = pos;
				AudioSource.Play();
			}
		}

        private void StartToFinish(BroAudioClip clip, PlaybackPreference pref)
        {
            OnFinishingStarted?.Invoke(ID, clip, pref);
            OnFinishingStarted = null;
        }

        #region Stop Overloads
        public virtual void Stop() 
            => Stop(UseLibraryManagerSetting);
        public virtual void Stop(float fadeOut) 
            => Stop(fadeOut, null);
        public virtual void Stop(Action onFinishStopping) 
            => Stop(UseLibraryManagerSetting, onFinishStopping);
        public virtual void Stop(float fadeOut, Action onFinished) 
            => Stop(fadeOut, default, onFinished);
        #endregion
        public virtual void Stop(float overrideFade, StopMode stopMode,Action onFinished)
        {
            if (IsStopping && overrideFade != Immediate)
            {
                return;
            }

            _isReadyToPlay = false;
            _stopMode = stopMode;
            this.StartCoroutineAndReassign(StopControl(overrideFade, stopMode, onFinished), ref _playbackControlCoroutine);
        }

		private IEnumerator StopControl(float overrideFade, StopMode stopMode, Action onFinished)
        {
            if (ID <= 0 || !IsPlaying)
            {
                onFinished?.Invoke();
                yield break;
            }

			#region FadeOut
			if (HasFading(CurrentClip.FadeOut,overrideFade,out float fadeTime))
            {
                IsStopping = true;
                if (IsFadingOut)
                {
                    // 如果已經在FadeOut了(也就是剛剛的Play已快結束)，就等它FadeOut不強制停止
                    float endTime = AudioSource.clip.length - CurrentClip.EndPosition;
                    yield return new WaitUntil(() => AudioSource.time >= endTime);
                }
                else
                {
                    yield return Fade(0f, fadeTime, VolumeControl.Clip,SoundManager.FadeOutEase);
                }
            }
			#endregion

			switch (stopMode)
			{
				case StopMode.Stop:
                    EndPlaying();
                    break;
				case StopMode.Pause:
                    AudioSource.Pause();
                    AddResumablePlayer();
                    break;
				case StopMode.Mute:
                    TrackVolumeBeforeMute = TrackVolume;
                    this.SetVolume(0f, 0f);
                    AddResumablePlayer();
                    break;
			}
            IsStopping = false;
            onFinished?.Invoke();
		}

        private bool HasFading(float clipFade, float overrideFade, out float fadeTime)
        {
            fadeTime = clipFade;
            if (overrideFade != UseLibraryManagerSetting)
            {
                fadeTime = overrideFade;
            }
            return fadeTime > Immediate;
        }

        private void AddResumablePlayer()
		{
            ResumablePlayers ??= new Dictionary<int, AudioPlayer>();
            ResumablePlayers[ID] = this;
        }

        private void RemoveFromResumablePlayer()
		{
            ResumablePlayers?.Remove(ID);
        }

        private void EndPlaying()
		{
            ID = -1;
			_clipVolume = DefaultClipVolume;
			_trackVolume = DefaultTrackVolume;
			_mixerDecibelVolume = DefaultMixerDecibelVolume;
            _stopMode = default;
            TrackVolumeBeforeMute = DefaultTrackVolume;

            AudioSource.Stop();
			AudioSource.clip = null;
			CurrentClip = null;
			this.StartCoroutineAndReassign(Recycle(), ref _recycleCoroutine);
			this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer(); 
        }
	}
}
