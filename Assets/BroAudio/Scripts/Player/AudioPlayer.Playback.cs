using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        public static Dictionary<int, AudioPlayer> ResumablePlayers = null;

        public event Action<int,BroAudioClip,PlaybackPreference> OnFinishingOneRound = null;
        public event Func<PlaybackPreference,PlaybackPreference> DecoratePlaybackPreference = null;

        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine = null;
        private Coroutine _trackVolumeControlCoroutine = null;
        private Coroutine _recycleCoroutine = null;
        private bool _isReadyToPlay = false;

        public void Play(int id, BroAudioClip clip, PlaybackPreference pref,bool waitForChainingMethod = true)
        {
            ID = id;
            CurrentClip = clip;
            _isReadyToPlay = true;
            IsStopping = false;
            this.SafeStopCoroutine(_recycleCoroutine);

            if(waitForChainingMethod)
			{
				ExecuteAfterChainingMethod(() =>
                {
                    if (_isReadyToPlay)
					{
						StartPlaying();
					}
					else
                    {
                        EndPlaying();
                    }
                });
            }
            else
			{
                StartPlaying();
            }

			void StartPlaying()
			{
                DecoratePref(ref pref);
                this.StartCoroutineAndReassign(PlayControl(clip, pref), ref _playbackControlCoroutine);
			}

            void ExecuteAfterChainingMethod(Action action)
            {
                AsyncTaskExtension.DelayDoAction(AsyncTaskExtension.MillisecondInSeconds, action);
            }

			PlaybackPreference DecoratePref(ref PlaybackPreference decoPref)
			{
                if(DecoratePlaybackPreference != null)
				{
                    decoPref = DecoratePlaybackPreference.Invoke(decoPref);
                }
				
				return decoPref;
			}
		}

        private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference pref)
        {
            if (pref.PlayerWaiter != null)
			{
                yield return new WaitUntil(() => pref.PlayerWaiter.IsFinished);
                pref.DisposeWaiter();
			}

            if (clip.Delay > 0)
			{
                yield return new WaitForSeconds(clip.Delay);
            }

            AudioSource.clip = clip.AudioClip;
            AudioSource.priority = pref.Entity.Priority;
            SetPitch(pref.Entity.Pitch);
            SetSpatial(pref);
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
                    //FadeIn start from here
                    yield return Fade(clip.Volume, fadeIn, fader,pref.FadeInEase);
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = clip.Volume;
                }
                #endregion

                if (pref.Entity.SeamlessLoop)
				{
                    pref.ApplySeamlessFade();
				}
                
                #region FadeOut
                float endTime = AudioSource.clip.length - clip.EndPosition;

                if (HasFading(clip.FadeOut, pref.FadeOut, out float fadeOut))
                {
                    yield return new WaitUntil(() => (endTime - AudioSource.time) <= fadeOut);
                    IsFadingOut = true;
                    OnFinishOneRound(clip, pref);
                    //FadeOut start from here
                    yield return Fade(0f, fadeOut, fader,pref.FadeOutEase);
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => AudioSource.time >= endTime || !AudioSource.isPlaying);
                    OnFinishOneRound(clip, pref);
                }
                #endregion
            } while (pref.Entity.Loop);

            EndPlaying();

			void PlayFromPos(float pos)
			{
				AudioSource.Stop();
				AudioSource.time = pos;
				AudioSource.Play();
			}
		}

		private void OnFinishOneRound(BroAudioClip clip, PlaybackPreference pref)
        {
            OnFinishingOneRound?.Invoke(ID, clip, pref);
            OnFinishingOneRound = null;
        }

        #region Stop Overloads
        public void Stop() 
            => Stop(UseEntitySetting);
        public void Stop(float fadeOut) 
            => Stop(fadeOut, null);
        public void Stop(Action onFinished) 
            => Stop(UseEntitySetting, onFinished);
        public void Stop(float fadeOut, Action onFinished) 
            => Stop(fadeOut, default, onFinished);
        #endregion
        public void Stop(float overrideFade, StopMode stopMode,Action onFinished)
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
            if (overrideFade != UseEntitySetting)
            {
                fadeTime = overrideFade;
            }
            return fadeTime > Immediate;
        }

        private void AddResumablePlayer()
		{
            if(ResumablePlayers == null)
                ResumablePlayers = new Dictionary<int, AudioPlayer>();
            ResumablePlayers[ID] = this;
        }

        private void RemoveFromResumablePlayer()
		{
            ResumablePlayers?.Remove(ID);
        }

        private void EndPlaying()
        {
            ID = -1;
            _stopMode = default;
            ResetVolume();

            AudioSource.Stop();
            AudioSource.clip = null;
            CurrentClip = null;
            ResetSpatial();

            this.StartCoroutineAndReassign(Recycle(), ref _recycleCoroutine);
            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer();
        }
    }
}
