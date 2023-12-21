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
                AsyncTaskExtension.DelayInvoke(AsyncTaskExtension.MillisecondInSeconds, action);
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
            SetPitch(pref.Entity);
            SetSpatial(pref);
            if(!pref.IsDominaor)
			{
                SetEffect(pref.AudioTypePlaybackPref.EffectType, SetEffectMode.Override);
                this.SafeStopCoroutine(_trackVolumeControlCoroutine);
                TrackVolume = pref.AudioTypePlaybackPref.Volume;
            }

            VolumeControl fader = VolumeControl.Clip;
            ClipVolume = 0f;
            float targetVolume = GetTargetVolume();
            RemoveFromResumablePlayer();

            // AudioSource.clip.samples returns the time samples length, not the data samples , so we only need normal sample rate and don't need to multiply the channel count.
            int sampleRate = clip.AudioClip.frequency; 
            

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
                int endSample = (int)(AudioSource.clip.samples - (clip.EndPosition * sampleRate));

                
                if (HasFading(clip.FadeOut, pref.FadeOut, out float fadeOut))
                {
                    yield return new WaitUntil(() => endSample - AudioSource.timeSamples <= fadeOut * sampleRate);
                    IsFadingOut = true;
                    OnFinishOneRound(clip, pref);
                    //FadeOut start from here
                    yield return Fade(0f, fadeOut, fader,pref.FadeOutEase);
                    IsFadingOut = false;
                }
                else
                {
                    bool hasPlayed = false;
                    yield return new WaitUntil(() => HasEndPlaying(ref hasPlayed) || endSample - AudioSource.timeSamples <= 0);
                    OnFinishOneRound(clip, pref);
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
                float result = clip.Volume;
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

            this.StartCoroutineAndReassign(Recycle(), ref _recycleCoroutine);
            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer();
        }
    }
}
