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
        [Obsolete]
        public event Action<SoundID> OnEndPlaying
        {
            add => _onEnd += value;
            remove => _onEnd -= value;
        }

        private PlaybackPreference _pref;
        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine = null;

        private event Action<SoundID> _onEnd = null;
        private event Action<IAudioPlayer> _onUpdate = null;
        private event Action<IAudioPlayer> _onStart = null;

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

            if(PlaybackStartingTime == 0)
            {
                PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime;
            }
			
			if (_stopMode == default)
            {
                _clip = _pref.PickNewClip();
            }

			this.StartCoroutineAndReassign(PlayControl(), ref _playbackControlCoroutine);
		}

        private IEnumerator PlayControl()
        {
            if (!SoundManager.Instance.TryGetAudioTypePref(ID.ToAudioType(), out var audioTypePref))
            {
                Debug.LogError(LogTitle + $"The ID:{ID} is invalid");
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

                if (_clip.Delay > 0)
                {
                    yield return new WaitForSeconds(_clip.Delay);
                }

                AudioSource.clip = _clip.AudioClip;
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

            int sampleRate = _clip.AudioClip.frequency;
			do
            {
                StartPlaying();
                float targetClipVolume = _clip.Volume * _pref.Entity.GetMasterVolume();
                float elapsedTime = 0f;

                #region FadeIn
                if(HasFading(_clip.FadeIn, _pref.FadeIn, out float fadeIn))
                {
                    _clipVolume.SetTarget(targetClipVolume);
                    while (_clipVolume.Update(ref elapsedTime, fadeIn, _pref.FadeInEase))
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
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
                int endSample = AudioSource.clip.samples - GetSample(sampleRate, _clip.EndPosition);
                if (HasFading(_clip.FadeOut, _pref.FadeOut, out float fadeOut))
                {
                    while (endSample - AudioSource.timeSamples > fadeOut * sampleRate)
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
                    }

                    IsFadingOut = true;
                    TriggerSeamlessLoopReplay();
                    _clipVolume.SetTarget(0f);
                    elapsedTime = 0f;
                    while (_clipVolume.Update(ref elapsedTime, fadeOut, _pref.FadeOutEase))
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
                    }
                    IsFadingOut = false;
                }
                else
                {
                    bool hasPlayed = false;
                    while(!HasEndPlaying(ref hasPlayed) && endSample - AudioSource.timeSamples > 0)
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
                    }
                    TriggerSeamlessLoopReplay();
                }
                #endregion

                if (_pref.Entity.Loop)
                {
                    _pref.ResetFading();
                }
            } while (_pref.Entity.Loop);

            EndPlaying();

            void StartPlaying()
            {
                switch (_stopMode)
                {
                    case StopMode.Stop:
                        PlayFromPos();
                        break;
                    case StopMode.Pause:
                        AudioSource.UnPause();
                        break;
                    case StopMode.Mute:
                        if (!AudioSource.isPlaying)
                        {
                            PlayFromPos();
                        }
                        break;
                }
                _stopMode = default;
                _onStart?.Invoke(this);
                _onUpdate?.Invoke(this);
                _onStart = null;
            }

			void PlayFromPos()
			{
				AudioSource.Stop();
				AudioSource.timeSamples = GetStartSample();
				AudioSource.Play();
			}

            // more accurate than AudioSource.isPlaying
            bool HasEndPlaying(ref bool hasPlayed)
            {
                int currentSample = AudioSource.timeSamples;
                int startSample = GetStartSample();
                if (!hasPlayed)
                {
                    hasPlayed = currentSample > startSample;
                }

                return hasPlayed && currentSample <= startSample;
            }

            int GetStartSample() => (int)(_clip.StartPosition * sampleRate);
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
        void IAudioStoppable.UnPause()
            => this.UnPause(UseEntitySetting);
        void IAudioStoppable.UnPause(float fadeIn)
        {
            _pref.FadeIn = fadeIn;
            Play();
        }
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
			if (HasFading(_clip.FadeOut,overrideFade,out float fadeTime))
            {
                if (IsFadingOut)
                {
                    // if is fading out. then don't stop. just wait for it
                    AudioClip clip = AudioSource.clip;
                    float endSample = clip.samples - (_clip.EndPosition * clip.frequency);
                    while(AudioSource.timeSamples < endSample)
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
                    }
                }
                else
                {
                    float elapsedTime = 0f;
                    _clipVolume.SetTarget(0f);
                    while(_clipVolume.Update(ref elapsedTime, fadeTime, SoundManager.FadeOutEase))
                    {
                        yield return null;
                        _onUpdate?.Invoke(this);
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
            if (overrideFade > 0f)
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
            _clip = null;
            ResetSpatial();
            ResetEffect();

            _trackVolume.StopCoroutine();
            _audioTypeVolume.StopCoroutine();
            RemoveFromResumablePlayer();

            _onEnd?.Invoke(ID);
            _onEnd = null;

            OnSeamlessLoopReplay = null;
            ID = -1;
            Recycle();
		}

        public IAudioPlayer OnEnd(Action<SoundID> onEnd)
        {
            _onEnd -= onEnd;
            _onEnd += onEnd;
            return this;
        }

        public IAudioPlayer OnUpdate(Action<IAudioPlayer> onUpdate)
        {
            _onUpdate -= onUpdate;
            _onUpdate += onUpdate;
            return this;
        }

        public IAudioPlayer OnStart(Action<IAudioPlayer> onStart)
        {
            _onStart -= onStart;
            _onStart += onStart;
            return this;
        }
    }
}
