using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using Ami.BroAudio.Tools;
using Ami.BroAudio.Data;
using UnityEngine.Audio;
using Ami.Extension;
using static Ami.Extension.TimeExtension;
using static Ami.BroAudio.Editor.AudioSourcePreviewStrategyExtension;

namespace Ami.BroAudio.Editor
{
    public class AudioSourcePreviewStrategy : EditorPreviewStrategy
    {
        public const int AudioSourceCount = 2;
        private enum MuteState { None, On, Off }
        private struct AudioSourceContent
        {
            public AudioSource Source;
            public EditorVolumeTransporter VolumeTransporter;
        }
        
        private readonly AudioSourceContent[] _audioSources = new AudioSourceContent[AudioSourceCount];
        private int _currentAudioSourceIndex = -1;
        private PreviewRequest _currentRequest;
        private AudioMixer _mixer;
        private MuteState _previousMuteState = MuteState.None;
        private TaskCompletionSource<bool> _playbackCompletionSource;
        private double _previewDspTime;
        private double _nextPreviewDspTime;
        private float _crossfadeTime;

        private AudioSource CurrentEditorAudioSource => _currentAudioSourceIndex >= 0 ? _audioSources[_currentAudioSourceIndex].Source : null;

        public AudioSourcePreviewStrategy()
        {
            _mixer = Resources.Load<AudioMixer>(BroEditorUtility.EditorAudioMixerPath);
        }

        public override void UpdatePreview()
        {
            base.UpdatePreview();
            if (_currentRequest != null && CurrentEditorAudioSource)
            {
                UpdatePitch();
                if (CurrentEditorAudioSource.GetPreciseTime() >=
                    _currentRequest.AbsoluteEndPosition || !CurrentEditorAudioSource.isPlaying)
                {
                    _playbackCompletionSource?.TrySetResult(true);
                }
            }
        }
        
        private void UpdatePitch()
        {
            AudioSource audioSource = CurrentEditorAudioSource;
            bool isPitchChanged = !Mathf.Approximately(_currentRequest.Pitch, audioSource.pitch);
            audioSource.pitch = _currentRequest.Pitch;
            if (isPitchChanged)
            {
                var remainingTime = (_currentRequest.AbsoluteEndPosition - audioSource.GetPreciseTime()) / _currentRequest.Pitch;
                _nextPreviewDspTime = AudioSettings.dspTime + remainingTime;
                audioSource.SetScheduledEndTime(_nextPreviewDspTime);
                var nextAudioSource = GetNextAudioSource(out _).Source;
                if (nextAudioSource.clip != null)
                {
                    double nextStartTime = _crossfadeTime > 0
                        ? _nextPreviewDspTime - _crossfadeTime / _currentRequest.Pitch
                        : _nextPreviewDspTime;
                    nextAudioSource.SetScheduledStartTime(nextStartTime);
                }
            }
        }

        public override async void Play(PreviewRequest req, ReplayRequest replayRequest = null)
        {
            try
            {
                await PlayClipByAudioSourceAsync(req, replayRequest);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipByAudioSourceAsync(PreviewRequest req, ReplayRequest replayRequest)
        {
            if(req.AudioClip == null) return;
            
            Stop();
            for (int i = 0; i < _audioSources.Length; i++)
            {
                _audioSources[i] = InstantiateAudioSource(i);
            }
            _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;
            EditorUtility.audioMasterMute = false;
            _mixer.SetAutoSuspend(false);
            
            _currentRequest = req;
            var audioSourceData = GetNextAudioSource(out _currentAudioSourceIndex);
            var audioSource = audioSourceData.Source;
            audioSource.SetPreviewRequest(req);
            
            var volumeTransporter = audioSourceData.VolumeTransporter;
            volumeTransporter.Init(req);

            _previewDspTime = AudioSettings.dspTime;
            double startDspTime = _previewDspTime + AudioConstant.MixerWarmUpTime;
            audioSource.PlayScheduled(startDspTime);
            audioSource.SetScheduledEndTime(startDspTime + req.Duration);
            await Task.Delay(SecToMs(AudioConstant.MixerWarmUpTime), CancellationSource.Token);
            _previewDspTime +=  AudioConstant.MixerWarmUpTime;
            _nextPreviewDspTime = _previewDspTime + req.Duration;
            
            if (replayRequest != null)
            {
                ScheduleNextPlayback(replayRequest, req);
            }

            StartPlaybackIndicator(replayRequest != null);
            volumeTransporter.Start();
            
            await WaitForPlaybackCompletion();
            volumeTransporter.End();
            _previewDspTime = _nextPreviewDspTime;

            while (replayRequest != null && replayRequest.CanReplay())
            {
                await AudioSourceReplay(req, replayRequest);
            }
            
            DestroyPreviewAudioSourceAndCancelTask();
        }

        private async Task WaitForPlaybackCompletion()
        {
            _playbackCompletionSource = new TaskCompletionSource<bool>();

            using (CancellationSource.Token.Register(() => _playbackCompletionSource?.TrySetCanceled()))
            {
                await _playbackCompletionSource.Task;
            }
        }

        private async Task AudioSourceReplay(PreviewRequest req, ReplayRequest replayReq)
        {
            EndPlaybackIndicator();
            var previousSource = _audioSources[_currentAudioSourceIndex];
            previousSource.VolumeTransporter.End();

            float activeCrossfadeTime = _crossfadeTime;
            bool hadCrossfade = activeCrossfadeTime > 0;

            // The previous scheduled audioSource has started at this point, we can just renew the process and prepare the next one
            replayReq.Start();
            req.SetReplay(replayReq);

            if (hadCrossfade)
            {
                _nextPreviewDspTime = _previewDspTime + req.Duration - activeCrossfadeTime / req.Pitch;
            }
            else
            {
                _nextPreviewDspTime = _previewDspTime + req.Duration;
            }

            var currentSource = GetNextAudioSource(out _currentAudioSourceIndex);
            currentSource.Source.pitch = replayReq.Pitch;
            currentSource.Source.SetScheduledEndTime(_nextPreviewDspTime);

            if (hadCrossfade)
            {
                currentSource.VolumeTransporter.UpdateOngoingPlayback(req, activeCrossfadeTime);
            }
            else
            {
                currentSource.VolumeTransporter.Init(req);
                currentSource.VolumeTransporter.Start();
            }

            StartPlaybackIndicator();
            if (replayReq.CanReplay())
            {
                ScheduleNextPlayback(replayReq, req);
            }

            await WaitForPlaybackCompletion();
            _previewDspTime = _nextPreviewDspTime;
        }
        
        private void ScheduleNextPlayback(ReplayRequest replayRequest, PreviewRequest req)
        {
            var source = GetNextAudioSource(out _);
            float crossfadeTime = replayRequest.CrossfadeTime;
            bool hasCrossfade = crossfadeTime > 0;
            _crossfadeTime = crossfadeTime;

            source.VolumeTransporter.SetStartVolume(req);
            var audioSource = source.Source;
            audioSource.ScheduleReplay(replayRequest);

            if (hasCrossfade)
            {
                _currentRequest.FadeOut = crossfadeTime;
                var runtimeSetting = BroEditorUtility.RuntimeSetting;
                _audioSources[_currentAudioSourceIndex].VolumeTransporter
                    .SetFadeEases(runtimeSetting.SeamlessFadeInEase, runtimeSetting.SeamlessFadeOutEase);

                double crossfadeDspOffset = crossfadeTime / replayRequest.Pitch;
                double scheduledStart = _nextPreviewDspTime - crossfadeDspOffset;
                audioSource.PlayScheduled(scheduledStart);
                audioSource.SetScheduledEndTime(scheduledStart + replayRequest.GetDuration());
                StartCrossfadeVolumeTransitionAsync(source, replayRequest, crossfadeTime);
            }
            else
            {
                audioSource.PlayScheduled(_nextPreviewDspTime);
                audioSource.SetScheduledEndTime(_nextPreviewDspTime + replayRequest.GetDuration());
            }
        }

        private async void StartCrossfadeVolumeTransitionAsync(AudioSourceContent nextSource, ReplayRequest replayReq, float crossfadeTime)
        {
            try
            {
                var crossfadeReq = new PreviewRequest(replayReq.Clip)
                {
                    MasterVolume = replayReq.MasterVolume,
                    Pitch = replayReq.Pitch,
                    FadeIn = crossfadeTime,
                };
                var runtimeSetting = BroEditorUtility.RuntimeSetting;
                nextSource.VolumeTransporter.SetFadeEases(runtimeSetting.SeamlessFadeInEase, runtimeSetting.SeamlessFadeOutEase);
                nextSource.VolumeTransporter.Init(crossfadeReq);

                double crossfadeStartDspTime = _nextPreviewDspTime - crossfadeTime / _currentRequest.Pitch;
                double delaySeconds = crossfadeStartDspTime - AudioSettings.dspTime;
                if (delaySeconds > 0)
                {
                    await Task.Delay(SecToMs(delaySeconds), CancellationSource.Token);
                }

                nextSource.VolumeTransporter.Start();
            }
            catch (OperationCanceledException) { }
        }
        
        private AudioSourceContent InstantiateAudioSource(int index)
        {
            string trackName = BroName.GenericTrackName + (index + 1).ToString();
            var gameObj = new GameObject("PreviewAudioClip");
            gameObj.tag = "EditorOnly";
            gameObj.hideFlags = HideFlags.HideAndDontSave;
            var audioSource = gameObj.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = GetEditorTrack(_mixer, trackName);
            audioSource.reverbZoneMix = 0f;
            audioSource.playOnAwake = false;

            var volumeTransporter = new EditorVolumeTransporter(_mixer, trackName);
            return new AudioSourceContent() { Source = audioSource, VolumeTransporter = volumeTransporter };
        }

        private void DestroyPreviewAudioSourceAndCancelTask()
        {
            _mixer.SetAutoSuspend(true);
            if (_currentAudioSourceIndex >= 0 || _currentRequest != null)
            {
                CancelTask();

                for (int i = 0; i < AudioSourceCount; i++)
                {
                    var data = _audioSources[i];
                    var audioSource = data.Source;
                    if (audioSource)
                    {
                        audioSource.Stop();
                        UnityEngine.Object.DestroyImmediate(audioSource.gameObject);
                    }
                    data.VolumeTransporter?.End();
                    data.VolumeTransporter?.Dispose();
                    _audioSources[i] = default;
                }
                _currentAudioSourceIndex = -1;
                _crossfadeTime = 0f;

                EndPlaybackIndicator();
                TriggerOnFinished();
            }
        }

        public override void Stop()
        {
            DestroyPreviewAudioSourceAndCancelTask();

            if (_previousMuteState != MuteState.None)
            {
                EditorUtility.audioMasterMute = _previousMuteState == MuteState.On;
                _previousMuteState = MuteState.None;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _currentRequest = null;
            DestroyPreviewAudioSourceAndCancelTask();
            _mixer.SetAutoSuspend(true);
            _mixer = null;
        }
        
        private AudioSourceContent GetNextAudioSource(out int newIndex)
        {
            newIndex = GetNextIndex(_currentAudioSourceIndex);
            return _audioSources[newIndex];
        }
    }
    
    public static class AudioSourcePreviewStrategyExtension
    {
        private const string MixerSuspendFieldName = "m_EnableSuspend";
        
        public static AudioMixerGroup GetEditorTrack(AudioMixer mixer, string name)
        {
            var tracks = mixer != null ? mixer.FindMatchingGroups(name) : null;
            if (tracks != null && tracks.Length > 0)
            {
                return tracks[0];
            }
            Debug.LogError($"Can't find {BroName.EditorAudioMixerName}'s {name} audioMixerGroup, the fading and extra volume is not applied to the preview");
            return null;
        }
        
        public static void SetAutoSuspend(this AudioMixer mixer, bool enable)
        {
            if (mixer)
            {
                SerializedObject serializedMixer = new SerializedObject(mixer);
                serializedMixer.Update();
                serializedMixer.FindProperty(MixerSuspendFieldName).boolValue = enable;
                serializedMixer.ApplyModifiedProperties();
            }
        }
        
        public static void SetPreviewRequest(this AudioSource audioSource, PreviewRequest req)
        {
            if (audioSource)
            {
                audioSource.clip = req.AudioClip;
                audioSource.timeSamples = req.AudioClip.GetTimeSample(req.StartPosition);
                audioSource.pitch = req.Pitch;
            }
        }
        
        public static void ScheduleReplay(this AudioSource audioSource, ReplayRequest req)
        {
            if (audioSource)
            {
                audioSource.Stop();
                audioSource.clip = req.GetAudioClipForScheduling();
                audioSource.timeSamples = req.StartSample;
                audioSource.pitch = req.Pitch;
            }
        }

        public static int GetNextIndex(int index)
        {
            int newIndex = index + 1;
            newIndex %= AudioSourcePreviewStrategy.AudioSourceCount;
            return newIndex;
        }
    }
}