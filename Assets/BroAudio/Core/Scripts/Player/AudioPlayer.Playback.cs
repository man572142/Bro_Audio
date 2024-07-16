using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        public delegate void SeamlessLoopReplay(int id, PlaybackPreference pref, EffectType effectType, float trackVolume, float pitch);

        public static Dictionary<int, AudioPlayer> ResumablePlayers = null;

        public event SeamlessLoopReplay OnSeamlessLoopReplay;
        public event Action<SoundID> OnEndPlaying;

        private PlaybackPreference _pref;
        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine = null;

        public int PlaybackStartingTime { get; private set; }

        public void SetPlaybackData(int id, PlaybackPreference pref)
        {
            ID = id;
            _pref = pref;
        }

        public void Play()
        {
            if(IsStopping || ID <= 0 || _pref.Entity == null)
            {
                return;
            }

			PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime;
			if (_stopMode == default)
            {
                CurrentClip = _pref.Entity.PickNewClip();
            }

			this.StartCoroutineAndReassign(PlayControl(), ref _playbackControlCoroutine);
		}

        private IEnumerator PlayControl()
        {
            if (!SoundManager.Instance.TryGetAudioTypePref(ID.ToAudioType(), out var audioTypePref))
            {
                Debug.LogError(Utility.LogTitle + $"The ID:{ID} is invalid");
                yield break;
            }

            if(!RemoveFromResumablePlayer()) // if is not resumable (not paused)
            {
                if (TryGetDecorator<MusicPlayer>(out var musicPlayer))
                {
                    AudioSource.reverbZoneMix = 0f;
                    AudioSource.priority = AudioConstant.HighestPriority;
                    musicPlayer.Transition(ref _pref);
                    while(musicPlayer.IsWaitingForTransition)
                    {
                        yield return null;
                    }
                }

                if (CurrentClip.Delay > 0)
                {
                    yield return new WaitForSeconds(CurrentClip.Delay);
                }

                AudioSource.clip = CurrentClip.AudioClip;
                AudioSource.priority = _pref.Entity.Priority;

                SetInitialPitch(_pref.Entity, audioTypePref);
                SetSpatial(_pref);

                if (IsDominator)
                {
                    TrackType = AudioTrackType.Dominator;
                }
                else
                {
                    SetEffect(audioTypePref.EffectType, SetEffectMode.Add);
                }
            }

            if (audioTypePref.Volume != DefaultTrackVolume && !_audioTypeVolume.IsFading)
            {
                _audioTypeVolume.Complete(audioTypePref.Volume, false);
            }
            _clipVolume.Complete(0f, false);
            UpdateMixerVolume();
            AudioTrack = _getAudioTrack?.Invoke(TrackType);

            int sampleRate = CurrentClip.AudioClip.frequency;
			do
            {
                StartPlaying();
                float targetClipVolume = CurrentClip.Volume * _pref.Entity.GetMasterVolume();
                float elapsedTime = 0f;

                #region FadeIn
                if(HasFading(CurrentClip.FadeIn, _pref.FadeIn, out float fadeIn))
                {
                    _clipVolume.SetTarget(targetClipVolume);
                    while (_clipVolume.Update(ref elapsedTime, fadeIn, _pref.FadeInEase))
                    {
                        yield return null;
                    }
                }
                else
                {
                    _clipVolume.Complete(targetClipVolume);
                }
                #endregion

                if (_pref.Entity.SeamlessLoop)
				{
                    _pref.ApplySeamlessFade();
				}

                #region FadeOut
                int endSample = AudioSource.clip.samples - GetSample(sampleRate, CurrentClip.EndPosition);
                if (HasFading(CurrentClip.FadeOut, _pref.FadeOut, out float fadeOut))
                {
                    while (endSample - AudioSource.timeSamples > fadeOut * sampleRate)
                    {
                        yield return null;
                    }

                    IsFadingOut = true;
                    TriggerSeamlessLoopReplay();
                    _clipVolume.SetTarget(0f);
                    elapsedTime = 0f;
                    while (_clipVolume.Update(ref elapsedTime, fadeOut, _pref.FadeOutEase))
                    {
                        yield return null;
                    }
                    IsFadingOut = false;
                }
                else
                {
                    bool hasPlayed = false;
                    while(!HasEndPlaying(ref hasPlayed) && endSample - AudioSource.timeSamples > 0)
                    {
                        yield return null;
                    }
                    TriggerSeamlessLoopReplay();
                }
                #endregion
            } while (_pref.Entity.Loop);

            EndPlaying();

            void StartPlaying()
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
                        if (!AudioSource.isPlaying)
                        {
                            PlayFromPos(CurrentClip.StartPosition);
                        }
                        break;
                }
                _stopMode = default;
            }

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

		private void TriggerSeamlessLoopReplay()
        {
            OnSeamlessLoopReplay?.Invoke(ID, _pref, CurrentActiveEffects, _trackVolume.Target, StaticPitch);
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
                    while(AudioSource.timeSamples < endSample)
                    {
                        yield return null;
                    }
                }
                else
                {
                    float elapsedTime = 0f;
                    _clipVolume.SetTarget(0f);
                    while(_clipVolume.Update(ref elapsedTime, fadeTime, SoundManager.FadeOutEase))
                    {
                        yield return null;
                    }
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
            PlaybackStartingTime = 0;
            _stopMode = default;
            _pref = default;
            ResetVolume();
            ResetPitch();

            AudioSource.Stop();
            AudioSource.clip = null;
            CurrentClip = null;
            ResetSpatial();
            ResetEffect();

            _trackVolume.StopCoroutine();
            _audioTypeVolume.StopCoroutine();
            RemoveFromResumablePlayer();
            OnEndPlaying?.Invoke(ID);
            OnEndPlaying = null;
            ID = -1;
            Recycle();
		}
	}
}
