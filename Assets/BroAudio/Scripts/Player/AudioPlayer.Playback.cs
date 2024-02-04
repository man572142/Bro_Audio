using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        public static Dictionary<int, AudioPlayer> ResumablePlayers = null;

        public event Action<int,PlaybackPreference,EffectType> OnFinishingOneRound = null;

        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine = null;
        private Coroutine _trackVolumeControlCoroutine = null;
        private bool _isReadyToPlay = false;

        public void Play(int id, PlaybackPreference pref,bool waitForChainingMethod = true)
        {
            ID = id;
            CurrentClip = pref.Entity.PickNewClip();
            _isReadyToPlay = true;
            IsStopping = false;

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
                this.StartCoroutineAndReassign(PlayControl(pref), ref _playbackControlCoroutine);
			}

            void ExecuteAfterChainingMethod(Action action)
            {
                AsyncTaskExtension.DelayInvoke(AsyncTaskExtension.MillisecondInSeconds, action);
            }
		}

        private IEnumerator PlayControl(PlaybackPreference pref)
        {
            if (pref.PlayerWaiter != null)
			{
                yield return new WaitUntil(() => pref.PlayerWaiter.IsFinished);
                pref.DisposeWaiter();
			}

            if (CurrentClip.Delay > 0)
			{
                yield return new WaitForSeconds(CurrentClip.Delay);
            }

            AudioSource.clip = CurrentClip.AudioClip;
            AudioSource.priority = pref.Entity.Priority;
            SetPitch(pref.Entity);
            SetSpatial(pref);

			if (TryGetDecorator<MusicPlayer>(out var musicPlayer))
			{
				AudioSource.reverbZoneMix = 0f;
                pref = musicPlayer.Transition(pref);
			}

			if (IsDominator)
			{
                TrackType = AudioTrackType.Dominator;
			}
            else
            {
                SetEffect(pref.AudioTypePlaybackPref.EffectType, SetEffectMode.Add);
                this.SafeStopCoroutine(_trackVolumeControlCoroutine);
                TrackVolume *= pref.AudioTypePlaybackPref.Volume;
            }

            AudioTrack = _getAudioTrack.Invoke(TrackType);
            VolumeControl fader = VolumeControl.Clip;
            ClipVolume = 0f;
            float targetVolume = GetTargetVolume();
            RemoveFromResumablePlayer();

            // AudioSource.clip.samples returns the time samples length, not the data samples, so we don't need to multiply the channel count.
            int sampleRate = CurrentClip.AudioClip.frequency; 
            
            do
            {
                switch (_stopMode)
                {
                    case StopMode.Stop:
                        PlayFromPos(CurrentClip.StartPosition);
                        break;
                    case StopMode.Pause:
                        AudioSource.UnPause();
                        break;
                    case StopMode.Mute:
                        this.SetVolume(TrackVolumeBeforeMute);
                        if (!AudioSource.isPlaying)
                        {
                            PlayFromPos(CurrentClip.StartPosition);
                        }
                        break;
                }
                _stopMode = default;
                #region FadeIn
                if (HasFading(CurrentClip.FadeIn, pref.FadeIn, out float fadeIn))
                {
                    IsFadingIn = true;
                    //FadeIn start from here
                    yield return Fade(targetVolume, fadeIn, fader,pref.FadeInEase);
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = targetVolume;
                }
                #endregion

                if (pref.Entity.SeamlessLoop)
				{
                    pref.ApplySeamlessFade();
				}

                #region FadeOut
                int endSample = (int)(AudioSource.clip.samples - (CurrentClip.EndPosition * sampleRate));
                if (HasFading(CurrentClip.FadeOut, pref.FadeOut, out float fadeOut))
                {
                    yield return new WaitUntil(() => endSample - AudioSource.timeSamples <= fadeOut * sampleRate);

                    IsFadingOut = true;
                    OnFinishOneRound(pref);
                    //FadeOut start from here
                    yield return Fade(0f, fadeOut, fader,pref.FadeOutEase);
                    IsFadingOut = false;
                }
                else
                {
                    bool hasPlayed = false;
                    yield return new WaitUntil(() => HasEndPlaying(ref hasPlayed) || endSample - AudioSource.timeSamples <= 0);
                    OnFinishOneRound(pref);
                }
                #endregion
            } while (pref.Entity.Loop);

            EndPlaying();

			void PlayFromPos(float pos)
			{
				AudioSource.Stop();
				AudioSource.timeSamples = (int)(pos * sampleRate);
				AudioSource.Play();
			}

            float GetTargetVolume()
			{
                float result = CurrentClip.Volume;
                if(pref.Entity.RandomFlags.Contains(RandomFlags.Volume))
				{
                    float masterVol = pref.Entity.MasterVolume + UnityEngine.Random.Range(-pref.Entity.VolumeRandomRange, pref.Entity.VolumeRandomRange);
                    result *= masterVol;
				}
                else
				{
                    result *= pref.Entity.MasterVolume;
				}
                return result;
			}

            // more accurate than AudioSource.isPlaying
            bool HasEndPlaying(ref bool hasPlayed)
            {
                int timeSample = AudioSource.timeSamples;
                if(!hasPlayed)
                {
                    hasPlayed = timeSample > 0;
                }
                
                return hasPlayed && timeSample == 0;
            }
		}

		private void OnFinishOneRound(PlaybackPreference pref)
        {
            OnFinishingOneRound?.Invoke(ID, pref, CurrentActiveEffects);
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
            if (ID <= 0 || !AudioSource.isPlaying)
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
                    // if is fading out. then don't stop. just wait for it
                    AudioClip clip = AudioSource.clip;
                    int sampleRate = clip.frequency * clip.channels;
                    float endSample = clip.samples - (CurrentClip.EndPosition * sampleRate);
                    yield return new WaitUntil(() => AudioSource.timeSamples >= endSample);
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
            ResumablePlayers = ResumablePlayers ?? new Dictionary<int, AudioPlayer>();
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
            ResetEffect();

            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer();
            Recycle();
		}
	}
}
