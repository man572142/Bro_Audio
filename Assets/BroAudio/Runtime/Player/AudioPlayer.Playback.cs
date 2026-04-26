using System;
using System.Collections;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        public delegate void PlaybackHandover(SoundID id, InstanceWrapper<AudioPlayer> wrapper, PlaybackPreference pref, EffectType effectType, float trackVolume, float pitch);

        public PlaybackHandover OnPlaybackHandover;

        private PlaybackPreference _pref;
        private StopMode _stopMode;
        private Coroutine _playbackControlCoroutine = null;

        private event Action<SoundID> _onEnd = null;
        private event Action<IAudioPlayer> _onUpdate = null;
        private event Action<IAudioPlayer> _onStart = null;
        private event Action<IAudioPlayer> _onPaused = null;

        public int PlaybackStartingTime { get; private set; }
        public bool HasStartedPlaying => PlaybackStartingTime > 0;
        private bool IsOnHold => _stopMode == StopMode.Pause && !HasStartedPlaying;

        public void SetPlaybackData(SoundID id, PlaybackPreference pref)
        {
            ID = id;
            _pref = pref;
        }

        public void Play()
        {
            if (IsStopping || IsOnHold || _pref.ScheduledStartTime > 0 || !ID.IsValid())
            {
                return;
            }

            PlayInternal();
        }

        private void PlayInternal()
        {
            if (!ValidatePlayback(ID.IsValid() && _pref.Entity != null, $"Cannot play audio. Invalid ID:{ID} or Entity is null.") ||
                !ValidatePlayback(SoundManager.Instance.TryGetAudioTypePref(ID.ToAudioType(), out var audioTypePref), $"Cannot play audio. Failed to get audio type preference for {ID.ToAudioType()}.") ||
                !ValidatePlayback(!HasStartedPlaying, "Audio Player wasn't cleaned up correctly"))
            {
                EndPlaying();
                return;
            }

            try
            {
                this.RestartCoroutine(PlayControl(audioTypePref), ref _playbackControlCoroutine);
            }
            catch (Exception ex)
            {
                EndPlaying();
                Debug.LogException(ex);
            }
        }

        private IEnumerator PlayControl(IAudioPlaybackPref audioTypePref)
        {
            if (!Mathf.Approximately(audioTypePref.Volume, DefaultTrackVolume) && !_audioTypeVolume.IsFading)
            {
                _audioTypeVolume.Complete(audioTypePref.Volume, false);
            }
            _clipVolume.Complete(0f, false);
            int sampleRate = _clip != null ? _clip.GetAudioClip().frequency : 0;
            bool hasScheduledPlay = false;
            if (!HasStartedPlaying) // we only do the following process when it's fresh
            {
                _clip = _pref.PickNewClip();
                if (_clip == null)
                {
                    EndPlaying();
                    yield break;
                }

                SetClipDelayIfNotScheduled();
#if PACKAGE_ADDRESSABLES
                if (_clip is BroAudioClip broAudioClip && broAudioClip.IsAddressablesAvailable() && !broAudioClip.IsLoaded)
                {
                    if (!SoundManager.Instance.Setting.AutomaticallyLoadAddressableAudioClips)
                    {
                        LogNotPreloadedMessage(broAudioClip);
                    }
                    yield return WaitForAddressablesToLoad(broAudioClip);
                }
#endif

                var audioClip = _clip.GetAudioClip();
                sampleRate = audioClip.frequency;
            if (!ValidatePlayback(audioClip != null, $"Audio Clip is not assigned in {ID}"))
            {
                EndPlaying();
                yield break;
            }
                AudioSource.clip = audioClip;
                AudioSource.priority = _pref.Entity.Priority;

                SetPlayPosition(sampleRate);
                SetInitialPitch(_pref.Entity, audioTypePref);
                SetSpatial(_pref);
#if !UNITY_WEBGL
                AudioTrack = MixerPool.GetTrack(TrackType);
#endif

                if (IsDominator)
                {
                    TrackType = AudioTrackType.Dominator;
                }
                else
                {
                    SetTrackEffect(audioTypePref.EffectType, SetEffectMode.Add);
                }

                SchedulePlayback(out hasScheduledPlay);
                if (hasScheduledPlay)
                {
                    yield return WaitForScheduledStartTime();
                }

                if (_decorators.TryGetDecorator<MusicPlayer>(out var musicPlayer))
                {
                    AudioSource.reverbZoneMix = 0f;
                    AudioSource.priority = AudioConstant.HighestPriority;
                    musicPlayer.DoTransition(ref _pref);
                    while (musicPlayer.IsWaitingForTransition)
                    {
                        yield return null;
                    }
                }
            }

            do
            {
                if (!hasScheduledPlay)
                {
                    StartPlaying(sampleRate);
                }

                if (!HasStartedPlaying)
                {
                    PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime;
                    _onStart?.Invoke(this);
                    _onStart = null;
                    _onUpdate?.Invoke(this);
                    hasScheduledPlay = false;
                }

                float targetClipVolume = _clip.Volume * _pref.Entity.GetMasterVolume();

                #region FadeIn
                if (_pref.HasFadeIn(_clip.FadeIn, out var fadeIn, out var fadeInEase))
                {
                    _clipVolume.SetTarget(targetClipVolume);
                yield return Fade(_clipVolume, fadeIn, fadeInEase, _onUpdate);
                }
                else
                {
                    _clipVolume.Complete(targetClipVolume);
                }
                #endregion

                if (_pref.IsLoop(LoopType.SeamlessLoop))
                {
                    _pref.ScheduledStartTime = 0d;
                    _pref.ApplySeamlessFade();
                }

                #region FadeOut
                int endSample = AudioSource.clip.samples - GetSample(sampleRate, _clip.EndPosition);
            
                if (_pref.HasFadeOut(_clip.FadeOut, out float fadeOut, out var fadeOutEase))
                {
                    while (endSample - AudioSource.timeSamples > fadeOut * sampleRate)
                    {
                        yield return null;
                    _onUpdate?.Invoke(this);
                    }

                    TriggerPlaybackHandover();
                    _clipVolume.SetTarget(0f);
                yield return Fade(_clipVolume, fadeOut, fadeOutEase, _onUpdate);
                }
                else
                {
                    bool hasPlayed = false;
                    while (!HasEndPlaying(ref hasPlayed, endSample, sampleRate))
                    {
                        yield return null;
                    _onUpdate?.Invoke(this);
                    }
                    TriggerPlaybackHandover();
                }
                #endregion
            } while (_pref.IsLoop(LoopType.Loop) && CanLoopIfIsChainedMode());

            EndPlaying();
        }

        private void StartPlaying(int sampleRate)
        {
            switch (_stopMode)
            {
                case StopMode.Pause when HasStartedPlaying:
                    AudioSource.UnPause();
                    break;
                case StopMode.Mute when AudioSource.isPlaying:
                    break;
                case StopMode.Stop:
                case StopMode.Pause:
                case StopMode.Mute:
                    PlayFromPos(sampleRate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _stopMode = default;
        }

        private void PlayFromPos(int sampleRate)
        {
            SetPlayPosition(sampleRate);
            AudioSource.Play();
        }

        private void SetPlayPosition(int sampleRate)
        {
            AudioSource.Stop();
            AudioSource.timeSamples = GetSample(sampleRate, _clip.StartPosition);
        }

        // more accurate than AudioSource.isPlaying
        private bool HasEndPlaying(ref bool hasPlayed, int endSample, int sampleRate)
        {
            int currentSample = AudioSource.timeSamples;
            int startSample = GetSample(sampleRate, _clip.StartPosition);
            if (!hasPlayed)
            {
                hasPlayed = currentSample > startSample;
            }

            return hasPlayed && (currentSample <= startSample || currentSample >= endSample);
        }

        private void TriggerPlaybackHandover(bool isEnd = false)
        {
            if ((isEnd && !_pref.CanHandoverToEnd()) || (!isEnd && !_pref.CanHandoverToLoop()))
            {
                return;
            }

            var newPref = _pref;
            if (newPref.IsChainedMode())
            {
                newPref.ChainedModeStage = isEnd ? PlaybackStage.End : PlaybackStage.Loop;
            }

            ClearScheduleEndEvents(); // it should be rescheduled in the new player
            OnPlaybackHandover?.Invoke(ID, _instanceWrapper, newPref, CurrentActiveTrackEffects, _trackVolume.Target, StaticPitch);
            OnPlaybackHandover = null;
            _instanceWrapper = null; // the instance has been transferred to the new player
        }

        private static bool ValidatePlayback(bool condition, string message, UnityEngine.Object context = null)
        {
            if (!condition)
            {
                Debug.LogError(LogTitle + message, context);
            }
            return condition;
        }

        #region Stop Overloads
        void IAudioStoppable.Pause()
            => this.Pause(FadeData.UseClipSetting);
        void IAudioStoppable.Pause(float fadeOut)
            => Stop(fadeOut, StopMode.Pause, null);
        void IAudioStoppable.UnPause()
            => this.UnPause(FadeData.UseClipSetting);
        void IAudioStoppable.UnPause(float fadeIn)
        {
            if (_stopMode != StopMode.Pause)
            {
                Debug.LogWarning(LogTitle + $"Cannot UnPause: The player is not paused. Sound:{ID}", this);
                return;
            }
            _pref.SetNextFadeIn(fadeIn);
            PlayInternal();
        }
        void IAudioStoppable.Stop()
            => this.Stop(FadeData.UseClipSetting);
        void IAudioStoppable.Stop(float fadeOut)
            => this.Stop(fadeOut, null);
        void IAudioStoppable.Stop(Action onFinished)
            => this.Stop(FadeData.UseClipSetting, onFinished);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished)
            => Stop(fadeOut, StopMode.Stop, onFinished);
        #endregion
        public void Stop(float overrideFade, StopMode stopMode, Action onFinished)
        {
            if (IsStopping && !Mathf.Approximately(overrideFade, FadeData.Immediate))
            {
                return;
            }

            bool isPlaying = AudioSource.isPlaying;
            if (stopMode == StopMode.Pause && !isPlaying)
            {
                bool hasPaused = _stopMode == StopMode.Pause;
                _stopMode = StopMode.Pause;
                if (!hasPaused)
                {
                    _onPaused?.Invoke(this);
                }
                return;
            }

            if (!ID.IsValid() || !isPlaying)
            {
                onFinished?.Invoke();
                EndPlaying();
                return;
            }

            this.RestartCoroutine(StopControl(overrideFade, stopMode, onFinished), ref _playbackControlCoroutine);
        }

        private IEnumerator StopControl(float overrideFade, StopMode stopMode, Action onFinished)
        {
            _stopMode = stopMode;
            IsStopping = true;
            _pref.SetNextFadeOut(overrideFade);

            TriggerPlaybackHandover(isEnd: true);
            #region FadeOut
            if (_pref.HasFadeOut(_clip.FadeOut, out float fadeOut, out var fadeOutEase))
            {
                if (_clipVolume.IsFadingOut)
                {
                    // if it's fading out. then don't stop. just wait for it
                    var clip = AudioSource.clip;
                    float endSample = clip.samples - (_clip.EndPosition * clip.frequency);
                    while (AudioSource.timeSamples < endSample)
                    {
                        yield return null;
                        if (!OnUpdate())
                        {
                            yield break;
                        }
                    }
                }
                else
                {
                    _clipVolume.SetTarget(0f);
                    yield return Fade(_clipVolume, fadeOut, fadeOutEase, _onUpdate);
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
                    _onPaused?.Invoke(this);
                    break;
                case StopMode.Mute:
                    this.SetVolume(0f);
                    break;
            }
            IsStopping = false;
            onFinished?.Invoke();
        }

        private bool OnUpdate()
        {
            _onUpdate?.Invoke(this);
            return IsActive;
        }

        private void EndPlaying()
        {
            PlaybackStartingTime = 0;
            _stopMode = default;
            _pref = default;
            IsStopping = false;
            ResetVolume();
            ResetPitch();

            AudioSource.Stop();
            AudioSource.clip = null;
            _clip = null;
            ResetSpatial();
            ResetEffect();

            // Don't add StopCoroutine(_playbackCoroutine) here, as this method is typically called within it, and further processing after this method cannot be guaranteed.
            _trackVolume.StopCoroutine();
            _audioTypeVolume.StopCoroutine();

            _onEnd?.Invoke(ID);
            _onEnd = null;

            Recycle();
        }

        private bool CanLoopIfIsChainedMode()
        {
            return !_pref.IsChainedMode() || (_pref.IsChainedMode() && _pref.ChainedModeStage == PlaybackStage.Loop);
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

        public IAudioPlayer OnPause(Action<IAudioPlayer> onPause)
        {
            _onPaused -= onPause;
            _onPaused += onPause;
            return this;
        }

        public IAudioPlayer SetFadeInEase(Ease ease)
        {
            _pref.SetFadeInEase(ease);
            return this;
        }

        public IAudioPlayer SetFadeOutEase(Ease ease)
        {
            _pref.SetFadeOutEase(ease);
            return this;
        }

#if PACKAGE_ADDRESSABLES
        private void LogNotPreloadedMessage(BroAudioClip broAudioClip)
        {
            var logMessage = broAudioClip.IsLoading
                ? LogTitle +
                  $"Entity: '{ID}' is still loading. You should wait for it to finish before playback, or it <b>may have caused a playback delay</b>."
                : LogTitle +
                  $"Entity: '{ID}' is marked as Addressables but was not preloaded — it will be loaded on demand and <b>may have caused a playback delay</b>.\n" +
                  $"Call BroAudio.{nameof(BroAudio.LoadAssetAsync)}() before playback, or enable <b>Automatically Load Addressable Audio Clips</b> in Preferences to suppress this error.";

            Log(logMessage, SoundManager.Instance.Setting.AddressablesNonPreloadedLogLevel);
        }

        private IEnumerator WaitForAddressablesToLoad(BroAudioClip broAudioClip)
        {
            if (broAudioClip.IsLoading)
            {
                // Wait for the existing loading operation to complete
                yield return broAudioClip.GetCurrentOperationHandle();
            }
            else
            {
                // Start loading and wait for it no matter what the user has set
                yield return broAudioClip.LoadAssetAsync();

                var loadedAudioClip = _clip.GetAudioClip();
                if (loadedAudioClip == null)
                {
                    Debug.LogError(LogTitle + $"Failed to load addressable audio clip for {ID}");
                }
            }

            // Update tracking when playback starts
            SoundManager.Instance.UpdateLoadedEntityLastPlayedTime(ID);
        }
#endif
    }
}