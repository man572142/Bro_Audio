using UnityEngine;
using UnityEditor;
using System;
using System.Threading.Tasks;
using UnityEngine.Audio;
using Ami.Extension;
using static Ami.Extension.TimeExtension;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Editor
{
    public class AudioSourcePreviewStrategy : EditorPreviewStrategy
    {
        private const string MixerSuspendFieldName = "m_EnableSuspend";

        private AudioSource _currentEditorAudioSource;
        private PreviewRequest _currentRequest;
        private AudioMixerGroup _masterTrack;
        private EditorVolumeTransporter _volumeTransporter;
        private AudioMixer _mixer;
        private MuteState _previousMuteState = MuteState.None;

        private TaskCompletionSource<bool> _playbackCompletionSource;

        private enum MuteState { None, On, Off }

        public AudioSourcePreviewStrategy()
        {
            _mixer = Resources.Load<AudioMixer>(BroEditorUtility.EditorAudioMixerPath);;
            _volumeTransporter = new EditorVolumeTransporter(_mixer);
        }

        public override void UpdatePreview()
        {
            base.UpdatePreview();
            if (_currentRequest != null && _currentEditorAudioSource)
            {
                UpdatePitch();

                if (_currentEditorAudioSource.time >=
                    _currentRequest.AbsoluteEndPosition || !_currentEditorAudioSource.isPlaying)
                {
                    _playbackCompletionSource?.TrySetResult(true);
                }
            }
        }

        private void UpdatePitch()
        {
            bool isPitchChanged = !Mathf.Approximately(_currentRequest.Pitch, _currentEditorAudioSource.pitch);
            _currentEditorAudioSource.pitch = _currentRequest.Pitch;
            if (isPitchChanged)
            {
                var remainingTime = (_currentRequest.AbsoluteEndPosition - _currentEditorAudioSource.time) / _currentRequest.Pitch;
                _currentEditorAudioSource.SetScheduledEndTime(AudioSettings.dspTime + remainingTime);
            }
        }

        public override async void Play(PreviewRequest req, bool loop = false, ReplayData replayData = null)
        {
            try
            {
                await PlayClipByAudioSourceAsync(req, loop, replayData);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipByAudioSourceAsync(PreviewRequest req, bool selfLoop, ReplayData replayData)
        {
            if(req.AudioClip == null)
            {
                return;
            }

            Stop();
            ResetAndGetAudioSource(out var audioSource);

            SetAudioSource(ref audioSource, req);
            _currentRequest = req;
            _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;
            EditorUtility.audioMasterMute = false;
            SetMixerAutoSuspend(_mixer, false);

            _volumeTransporter.SetData(req);

            double startDspTime = AudioSettings.dspTime + AudioConstant.MixerWarmUpTime;
            audioSource.PlayScheduled(startDspTime);
            audioSource.SetScheduledEndTime(startDspTime + req.Duration);

            await Task.Delay(SecToMs(AudioConstant.MixerWarmUpTime), CancellationSource.Token);
            StartPlaybackIndicator(selfLoop || replayData != null);
            _volumeTransporter.Start();
            
            await WaitForPlaybackCompletion();
            _volumeTransporter.End();

            if (selfLoop)
            {
                while (true)
                {
                    await AudioSourceReplay(req);
                }
            }
            else if (replayData != null)
            {
                while (true)
                {
                    req.SetReplay(replayData.NewReplay());
                    await WaitForPlaybackCompletion();
                }
            }
            else
            {
                DestroyPreviewAudioSourceAndCancelTask();
            }
        }

        private async Task WaitForPlaybackCompletion()
        {
            _playbackCompletionSource = new TaskCompletionSource<bool>();

            using (CancellationSource.Token.Register(() => _playbackCompletionSource?.TrySetCanceled()))
            {
                await _playbackCompletionSource.Task;
            }
        }

        private void ResetAndGetAudioSource(out AudioSource result)
        {
            if (_currentEditorAudioSource)
            {
                CancelTask();
            }
            else
            {
                var gameObj = new GameObject("PreviewAudioClip");
                gameObj.tag = "EditorOnly";
                gameObj.hideFlags = HideFlags.HideAndDontSave;
                _currentEditorAudioSource = gameObj.AddComponent<AudioSource>();
            }
            result = _currentEditorAudioSource;
        }

        private void SetAudioSource(ref AudioSource audioSource, PreviewRequest req)
        {
            audioSource.clip = req.AudioClip;
            audioSource.playOnAwake = false;
            audioSource.timeSamples = GetSample(req.AudioClip.frequency, req.StartPosition);
            audioSource.pitch = req.Pitch;
            audioSource.outputAudioMixerGroup = GetEditorMasterTrack();
            audioSource.reverbZoneMix = 0f;
        }

        private async Task AudioSourceReplay(PreviewRequest req)
        {
            if (_currentEditorAudioSource != null)
            {
                EndPlaybackIndicator();
                if (_volumeTransporter.IsNewVolumeDifferentFromCurrent(req))
                {
                    _volumeTransporter.SetData(req);
                    await Task.Delay(SecToMs(AudioConstant.MixerWarmUpTime), CancellationSource.Token);
                }

                double dspTime = AudioSettings.dspTime;
                _currentEditorAudioSource.timeSamples = GetSample(req.AudioClip.frequency, req.StartPosition);
                _currentEditorAudioSource.PlayScheduled(dspTime);
                _currentEditorAudioSource.SetScheduledEndTime(dspTime + req.Duration);

                _volumeTransporter.Start();
                StartPlaybackIndicator();

                await WaitForPlaybackCompletion();
            }
        }

        private void DestroyPreviewAudioSourceAndCancelTask()
        {
            SetMixerAutoSuspend(_mixer, true);
            if (_currentEditorAudioSource)
            {
                CancelTask();

                _currentEditorAudioSource.Stop();
                UnityEngine.Object.DestroyImmediate(_currentEditorAudioSource.gameObject);
                EndPlaybackIndicator();
                _volumeTransporter.End();
                _volumeTransporter.Dispose();
                _currentEditorAudioSource = null;
                TriggerOnFinished();
            }
        }

        private AudioMixerGroup GetEditorMasterTrack()
        {
            if (_masterTrack == null)
            {
                var tracks = _mixer != null ? _mixer.FindMatchingGroups("Master") : null;
                if (tracks != null && tracks.Length > 0)
                {
                    _masterTrack = tracks[0];
                }
                else
                {
                    Debug.LogError("Can't find EditorBroAudioMixer's Master audioMixerGroup, the fading and extra volume is not applied to the preview");
                }
            }
            return _masterTrack;
        }

        private static void SetMixerAutoSuspend(AudioMixer mixer, bool enable)
        {
            if(mixer)
            {
                SerializedObject serializedMixer = new SerializedObject(mixer);
                serializedMixer.Update();
                serializedMixer.FindProperty(MixerSuspendFieldName).boolValue = enable;
                serializedMixer.ApplyModifiedProperties();
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
            _volumeTransporter?.Dispose();
            _volumeTransporter = null;
            _masterTrack = null;
            SetMixerAutoSuspend(_mixer, true);
            _mixer = null;
        }
    }
}