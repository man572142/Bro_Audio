using UnityEditor;
using Ami.BroAudio.Editor;
using System;
using System.Threading;
using UnityEngine;

namespace Ami.Extension
{
    public interface IEditorPreviewModule
    {
        PlaybackIndicatorUpdater PlaybackIndicator { get; }
        CancellationTokenSource CancellationSource { get; }
        void TriggerOnFinished();
        void CancelTask();
    }

    public class EditorPlayAudioClip : IEditorPreviewModule
    {
        public const string IgnoreSettingTooltip = "Right-click to play the audio clip directly";

        private static EditorPlayAudioClip _instance = null;
        public static EditorPlayAudioClip Instance
        {
            get
            {
                _instance ??= new EditorPlayAudioClip();
                return _instance;
            }
        }

        public PlaybackIndicatorUpdater PlaybackIndicator { get; private set; }

        public Action OnFinished;

        private readonly AudioSourcePreviewStrategy _audioSourceStrategy;
        private readonly AudioClipPreviewStrategy _audioClipStrategy;
        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        CancellationTokenSource IEditorPreviewModule.CancellationSource => _cancellationSource ??= new CancellationTokenSource();

        public EditorPlayAudioClip()
        {
            PlaybackIndicator = new PlaybackIndicatorUpdater();

            _audioSourceStrategy = new AudioSourcePreviewStrategy(this);
            _audioClipStrategy = new AudioClipPreviewStrategy(this);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public async void PlayClipByAudioSource(PreviewData clip, bool selfLoop = false, ReplayData replayData = default, bool isTransportIgnored = false)
        {
            try
            {
                StopAllClips();
                await _audioSourceStrategy.PlayAsync(clip, selfLoop, replayData, isTransportIgnored);
            }
            catch (OperationCanceledException) { }
        }

        public async void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
        {
            try
            {
                StopAllClips();
                await _audioClipStrategy.PlayAsync(audioClip, startSample, endSample, loop);
            }
            catch (OperationCanceledException) { }
        }

        public void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
        {
            int startSample = AudioExtension.GetTimeSample(audioClip, startTime);
            int endSample = AudioExtension.GetTimeSample(audioClip, endTime);
            PlayClip(audioClip, startSample, endSample, loop);
        }

        public void UpdatePreviewClipValues(float volume, float pitch, ITransport transport)
        {
            _audioSourceStrategy.UpdatePreviewClipValues(volume, pitch, transport);
        }

        public void StopAllClips()
        {
            _audioSourceStrategy.Stop();
            _audioClipStrategy.Stop();
        }

        void IEditorPreviewModule.TriggerOnFinished()
        {
            OnFinished?.Invoke();
            OnFinished = null;
        }

        void IEditorPreviewModule.CancelTask()
        {
            if (_cancellationSource != null && _cancellationSource.Token.CanBeCanceled)
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();
                _cancellationSource = null;
            }
        }

        public void AddPlaybackIndicatorListener(Action action)
        {
            RemovePlaybackIndicatorListener(action);
            PlaybackIndicator.OnUpdate += action;
            PlaybackIndicator.OnEnd += action;
        }

        public void RemovePlaybackIndicatorListener(Action action)
        {
            PlaybackIndicator.OnUpdate -= action;
            PlaybackIndicator.OnEnd -= action;
        }

        private void Dispose()
        {
            OnFinished = null;
            StopAllClips();
            PlaybackIndicator.Dispose();
            PlaybackIndicator = null;
            _instance = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.ExitingPlayMode)
            {
                Dispose();
            }
        }
    }
}