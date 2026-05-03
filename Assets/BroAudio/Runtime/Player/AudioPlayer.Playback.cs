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
        public delegate AudioPlayer HandoverPlayerFactory(PlaybackHandoverData handover);

        public HandoverPlayerFactory RequestNextPlayer;

        private PlaybackPreference _pref;
        private StopMode _stopMode;
        private Coroutine _playbackControlCoroutine = null;
        private Coroutine _handoverScheduleCoroutine = null;
        private AudioPlayer _nextPlayer;

        private event Action<SoundID> _onEnd = null;
        private event Action<IAudioPlayer> _onUpdate = null;
        private event Action<IAudioPlayer> _onStart = null;
        private event Action<IAudioPlayer> _onPaused = null;

        public int PlaybackStartingTime { get; private set; }
        public bool HasStartedPlaying => PlaybackStartingTime > 0;
        private bool IsOnHold => _stopMode == StopMode.Pause && !HasStartedPlaying;

        public void SetPlaybackData(SoundID id, PlaybackPreference pref, IBroAudioClip clip = null)
        {
            ID = id;
            _pref = pref;
            _clip = clip;
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
            _clip ??= _pref.PickNewClip(); // TODO: Add change clip for each loop option
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
            if (!ValidatePlayback(audioClip != null, $"Audio Clip is not assigned in {ID}"))
            {
                EndPlaying();
                yield break;
            }

            int sampleRate = audioClip.frequency;
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
            
            double dspTime = AudioSettings.dspTime;
            float pitch = AudioSource.pitch;
            double pitchAdjustedDuration = PitchAdjusted(_clip.GetPlayableDuration(), pitch);
            double endDspTime = _pref.ScheduledEndTime <= 0 ? dspTime + pitchAdjustedDuration : _pref.ScheduledEndTime;
            if (_pref.HasLoop() && _pref.ScheduledStartTime <= 0)
            {
                // TODO: might need a warmup time?
                _pref.ScheduledStartTime = dspTime;
                _pref.ScheduledEndTime = endDspTime;
            }

            SchedulePlayback();
            if (_secondsUntilScheduledStart > 0)
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
            
            if (_pref.ScheduledStartTime <= 0)
            {
                StartPlaying();
            }

            if (!HasStartedPlaying)
            {
                PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime;
                _onStart?.Invoke(this);
                _onStart = null;
                _onUpdate?.Invoke(this);
            }

            float targetClipVolume = _clip.Volume * _pref.Entity.GetMasterVolume();
            if (_pref.HasFadeIn(_clip.FadeIn, out var fadeIn, out var fadeInEase))
            {
                _clipVolume.SetTarget(targetClipVolume);
                yield return Fade(_clipVolume, fadeIn, fadeInEase, _onUpdate);
            }
            else
            {
                _clipVolume.Complete(targetClipVolume);
            }

            if (_pref.CanHandoverToLoop())
            {
                if (_pref.IsLoop(LoopType.SeamlessLoop))
                {
                    _pref.ApplySeamlessFade();
                }

                this.RestartCoroutine(ScheduleNextPlayback(endDspTime), ref _handoverScheduleCoroutine);
            }

            if (_pref.HasFadeOut(_clip.FadeOut, out float fadeOut, out var fadeOutEase))
            {
                double fadeOutDspTime = endDspTime - fadeOut;
                while (AudioSettings.dspTime < fadeOutDspTime)
                {
                    yield return null;
                    _onUpdate?.Invoke(this);
                }

                BeginHandover();
                _clipVolume.SetTarget(0f);
                yield return Fade(_clipVolume, fadeOut, fadeOutEase, _onUpdate);
            }
            else
            {
                while (AudioSettings.dspTime < endDspTime)
                {
                    yield return null;
                    _onUpdate?.Invoke(this);
                }
                BeginHandover();
            }
            EndPlaying();
        }

        private void StartPlaying()
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
                    AudioSource.Play();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _stopMode = default;
        }

        private void SetPlayPosition(int sampleRate)
        {
            AudioSource.Stop();
            AudioSource.timeSamples = GetSample(sampleRate, _clip.StartPosition);
        }
        
        private IEnumerator ScheduleNextPlayback(double endDspTime, bool isEnd = false)
        {
            var newPref = _pref;
            if (newPref.IsChainedMode())
            {
                newPref.ChainedModeStage = isEnd ? PlaybackStage.End : PlaybackStage.Loop;
            }
            
            double pitchAdjustedDuration = PitchAdjusted(_clip.GetPlayableDuration(), AudioSource.pitch);
            if (_pref.IsLoop(LoopType.SeamlessLoop) && newPref.HasFadeOut(_clip.FadeOut, out float fadeOut, out _))
            {
                double fadeOutDspTime = endDspTime - fadeOut;
                newPref.ScheduledStartTime = fadeOutDspTime;
                newPref.ScheduledEndTime = fadeOutDspTime + pitchAdjustedDuration;
            }
            else
            {
                newPref.ScheduledStartTime = endDspTime;
                newPref.ScheduledEndTime = endDspTime + pitchAdjustedDuration;
            }
            // TODO: calculate the real warmup time
            while (AudioSettings.dspTime < newPref.ScheduledStartTime - 0.1)
            {
                yield return null;
            }
            var handover = new PlaybackHandoverData()
            {
                ID = ID,
                Pref = newPref,
                Clip = (newPref.IsChainedMode() && newPref.ChainedModeStage != PlaybackStage.Loop) ? null : _clip,
                TrackEffect = CurrentActiveTrackEffects,
                TrackVolume = _trackVolume.Target,
                Pitch = StaticPitch,
            };
            _nextPlayer = RequestNextPlayer?.Invoke(handover);
            RequestNextPlayer = null;
        }

        private void BeginHandover(bool isEnd = false)
        {
            if (!CanHandover(isEnd))
            {
                return;
            }

            ClearScheduleEndEvents();
            _instanceWrapper.UpdateInstance(_nextPlayer);
            _nextPlayer.SetInstanceWrapper(_instanceWrapper);
            _instanceWrapper = null;
        }

        private bool CanHandover(bool isEnd = false) =>
            isEnd ? _pref.CanHandoverToEnd() : _pref.CanHandoverToLoop();

        private static double PitchAdjusted(double duration, float pitch) =>
            Mathf.Approximately(pitch, 0f) ? duration : duration / pitch;

        internal void ReceiveHandover(PlaybackHandoverData handover)
        {
            SetPlaybackData(handover.ID, handover.Pref, handover.Clip);
            SetVolumeInternal(_trackVolume, handover.TrackVolume, 0f);
            this.SetPitch(handover.Pitch);
            PlayInternal();
#if !UNITY_WEBGL
            SetTrackEffect(handover.TrackEffect, SetEffectMode.Override);
#endif
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

            BeginHandover(isEnd: true);
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
            
            _trackVolume.StopCoroutine();
            _audioTypeVolume.StopCoroutine();

            _onEnd?.Invoke(ID);
            _onEnd = null;

            Recycle();
        }

        private bool CanLoopIfIsChainedMode()
        {
            return !_pref.IsChainedMode() || _pref.ChainedModeStage == PlaybackStage.Loop;
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