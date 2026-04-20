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
            if (IsStopping || IsOnHold || !ID.IsValid())
            {
                return;
            }

            if (_pref.ScheduledStartTime > 0 && _playbackControlCoroutine != null)
            {
                return;
            }

            PlayInternal();
        }

        private void PlayInternal()
        {
            if (!ID.IsValid() || _pref.Entity == null
                       || !SoundManager.Instance.TryGetAudioTypePref(ID.ToAudioType(), out var audioTypePref))
            {
                Debug.LogError(LogTitle + $"Cannot play audio. Invalid ID:{ID} or Entity is null.");
                return;
            }
            try
            {
                this.StartCoroutineAndReassign(PlayControl(audioTypePref), ref _playbackControlCoroutine);
            }
            catch (Exception ex)
            {
                ClearEvents();
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

            bool isResumingFromPause = _stopMode == StopMode.Pause && HasStartedPlaying;
            double scheduledIterationStart = _pref.ScheduledStartTime;

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
            }

            double iterationStartDsp = scheduledIterationStart > 0d
                ? scheduledIterationStart
                : AudioSettings.dspTime;

            int startSample = GetSample(sampleRate, _clip.StartPosition);
            int endSample = AudioSource.clip.samples - GetSample(sampleRate, _clip.EndPosition);
            int trimSamples = endSample - startSample;
            float pitch = Mathf.Max(Mathf.Abs(AudioSource.pitch), 0.0001f);
            double loopDurationSec = trimSamples > 0 ? trimSamples / ((double)sampleRate * pitch) : 0d;
            double loopEndDsp;
            if (isResumingFromPause)
            {
                int remainingSamples = Mathf.Max(endSample - AudioSource.timeSamples, 0);
                loopEndDsp = AudioSettings.dspTime + remainingSamples / ((double)sampleRate * pitch);
            }
            else
            {
                loopEndDsp = iterationStartDsp + loopDurationSec;
            }
            bool useScheduledLoop = trimSamples > 0 && CanLoopIfIsChainedMode() &&
                                    (_pref.IsLoop(LoopType.Loop) || _pref.IsLoop(LoopType.SeamlessLoop));

            float targetClipVolume = _clip.Volume * _pref.Entity.GetMasterVolume();
            float elapsedTime = 0f;

            #region FadeIn
            if (_pref.HasFadeIn(_clip.FadeIn, out var fadeIn, out var fadeInEase))
            {
                _clipVolume.SetTarget(targetClipVolume);
                while (_clipVolume.Update(ref elapsedTime, fadeIn, fadeInEase))
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
                _clipVolume.Complete(targetClipVolume);
            }
            #endregion

            bool hasFadeOut = _pref.HasFadeOut(_clip.FadeOut, out float fadeOut, out var fadeOutEase);
            double fadeOutStartDsp = loopEndDsp - (hasFadeOut ? fadeOut : 0d);

            if (useScheduledLoop)
            {
                if (_pref.IsLoop(LoopType.SeamlessLoop))
                {
                    _pref.ApplySeamlessFade();
                    _pref.Entity.HasLoop(out _, out float transitionTime);
                    _pref.ScheduledStartTime = loopEndDsp - transitionTime;
                }
                else
                {
                    _pref.ScheduledStartTime = loopEndDsp;
                }

                AudioSource.SetScheduledEndTime(loopEndDsp);
                TriggerPlaybackHandover();
            }
            else
            {
                _pref.ScheduledStartTime = 0d;
            }

            #region FadeOut
            if (hasFadeOut)
            {
                while (AudioSettings.dspTime < fadeOutStartDsp)
                {
                    yield return null;
                    if (!OnUpdate())
                    {
                        yield break;
                    }
                }

                if (!useScheduledLoop)
                {
                    TriggerPlaybackHandover();
                }
                _clipVolume.SetTarget(0f);
                elapsedTime = 0f;
                IsFadingOut = true;
                while (_clipVolume.Update(ref elapsedTime, fadeOut, fadeOutEase))
                {
                    yield return null;
                    if (!OnUpdate())
                    {
                        yield break;
                    }
                }
                IsFadingOut = false;
            }
            else
            {
                while (AudioSettings.dspTime < loopEndDsp)
                {
                    yield return null;
                    if (!OnUpdate())
                    {
                        yield break;
                    }
                }
                if (!useScheduledLoop)
                {
                    TriggerPlaybackHandover();
                }
            }
            #endregion

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

            this.StartCoroutineAndReassign(StopControl(overrideFade, stopMode, onFinished), ref _playbackControlCoroutine);
        }

        private IEnumerator StopControl(float overrideFade, StopMode stopMode, Action onFinished)
        {
            _stopMode = stopMode;
            IsStopping = true;
            _pref.SetNextFadeOut(overrideFade);

            if (AudioSource.isPlaying)
            {
                AudioSource.SetScheduledEndTime(AudioSettings.dspTime + 1e9);
            }

            TriggerPlaybackHandover(isEnd: true);
            #region FadeOut
            if (_pref.HasFadeOut(_clip.FadeOut, out float fadeOut, out var fadeOutEase))
            {
                if (IsFadingOut)
                {
                    // if it's fading out. then don't stop. just wait for it
                    AudioClip clip = AudioSource.clip;
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
                    float elapsedTime = 0f;
                    _clipVolume.SetTarget(0f);
                    while (_clipVolume.Update(ref elapsedTime, fadeOut, fadeOutEase))
                    {
                        yield return null;
                        if (!OnUpdate())
                        {
                            yield break;
                        }
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
            IsFadingOut = false;
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