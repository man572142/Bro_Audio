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
        public delegate void SeamlessLoopReplay(int id, PlaybackPreference pref, EffectType effectType, float trackVolume, float pitch);

        public static Dictionary<int, AudioPlayer> ResumablePlayers = null;

        public event SeamlessLoopReplay OnSeamlessLoopReplay;
        public event Action<SoundID> OnEndPlaying;

        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine = null;
        private Coroutine _trackVolumeControlCoroutine = null;

        public int PlaybackStartingTime { get; private set; }

        public void Play(int id, PlaybackPreference pref)
        {
            if(IsStopping)
            {
                return;
            }

            ID = id;
			PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime;
			if (_stopMode == default)
            {
                CurrentClip = pref.Entity.PickNewClip();
            }

			this.StartCoroutineAndReassign(PlayControl(pref), ref _playbackControlCoroutine);
		}

        private IEnumerator PlayControl(PlaybackPreference pref)
        {
            IAudioPlaybackPref audioTypePlaybackPref = null;
            if (SoundManager.Instance.AudioTypePref.TryGetValue(ID.ToAudioType(), out var typePref))
            {
                audioTypePlaybackPref = typePref;
            }

            if(!RemoveFromResumablePlayer()) // if is not resumable (not paused)
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

                SetInitialPitch(pref.Entity, audioTypePlaybackPref);
                SetSpatial(pref);

                if (TryGetDecorator<MusicPlayer>(out var musicPlayer))
                {
                    AudioSource.reverbZoneMix = 0f;
                    AudioSource.priority = AudioConstant.HighestPriority;
                    pref = musicPlayer.Transition(pref);
                }

                if (IsDominator)
                {
                    TrackType = AudioTrackType.Dominator;
                }
                else
                {
                    SetEffect(audioTypePlaybackPref.EffectType, SetEffectMode.Add);
                }
            }

			this.SafeStopCoroutine(_trackVolumeControlCoroutine);
			TrackVolume = StaticTrackVolume * audioTypePlaybackPref.Volume;           
            ClipVolume = 0f;
            float targetClipVolume = CurrentClip.Volume * pref.Entity.GetMasterVolume();
            AudioTrack = _getAudioTrack?.Invoke(TrackType);

            int sampleRate = CurrentClip.AudioClip.frequency;
			VolumeControl fader = VolumeControl.Clip;
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
                        this.SetVolume(StaticTrackVolume);
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
                    yield return Fade(targetClipVolume, fadeIn, fader,pref.FadeInEase);
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = targetClipVolume;
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
                    TriggerSeamlessLoopReplay(pref);
                    //FadeOut start from here
                    yield return Fade(0f, fadeOut, fader,pref.FadeOutEase);
                    IsFadingOut = false;
                }
                else
                {
                    bool hasPlayed = false;
                    yield return new WaitUntil(() => HasEndPlaying(ref hasPlayed) || endSample - AudioSource.timeSamples <= 0);
                    TriggerSeamlessLoopReplay(pref);
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

		private void TriggerSeamlessLoopReplay(PlaybackPreference pref)
        {
            OnSeamlessLoopReplay?.Invoke(ID, pref, CurrentActiveEffects, StaticTrackVolume, StaticPitch);
            OnSeamlessLoopReplay = null;
        }

        #region Stop Overloads
        void IAudioStoppable.Pause()
            => this.Pause(UseEntitySetting);
        void IAudioStoppable.Pause(float fadeOut)
            => Stop(fadeOut, StopMode.Pause, null);
        void IAudioStoppable.Stop() 
            => this.Stop(UseEntitySetting);
        void IAudioStoppable.Stop(float fadeOut) 
            => this.Stop(fadeOut, null);
        void IAudioStoppable.Stop(Action onFinished) 
            => this.Stop(UseEntitySetting, onFinished);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) 
            => Stop(fadeOut, StopMode.Stop, onFinished);
        #endregion
        public void Stop(float overrideFade, StopMode stopMode,Action onFinished)
        {
            if (IsStopping && overrideFade != Immediate)
            {
                return;
            }

			if (ID <= 0 || !AudioSource.isPlaying)
			{
				onFinished?.Invoke();
				return;
			}

			this.StartCoroutineAndReassign(StopControl(overrideFade, stopMode, onFinished), ref _playbackControlCoroutine);
        }

		private IEnumerator StopControl(float overrideFade, StopMode stopMode, Action onFinished)
        {
			_stopMode = stopMode;
			IsStopping = true;

			#region FadeOut
			if (HasFading(CurrentClip.FadeOut,overrideFade,out float fadeTime))
            {
                if (IsFadingOut)
                {
                    // if is fading out. then don't stop. just wait for it
                    AudioClip clip = AudioSource.clip;
                    float endSample = clip.samples - (CurrentClip.EndPosition * clip.frequency);
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
                    StaticTrackVolume = TrackVolume;
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

        private bool RemoveFromResumablePlayer()
		{
            return ResumablePlayers != null ? ResumablePlayers.Remove(ID) : false;
        }

        private void EndPlaying()
        {   
            _stopMode = default;
            ResetVolume();
            ResetPitch();

            AudioSource.Stop();
            AudioSource.clip = null;
            CurrentClip = null;
            ResetSpatial();
            ResetEffect();

            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer();
            OnEndPlaying?.Invoke(ID);
            OnEndPlaying = null;
            ID = -1;
            Recycle();
		}
	}
}
