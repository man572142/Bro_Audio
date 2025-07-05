using UnityEngine;
using UnityEditor;
using System;
using Ami.BroAudio.Editor;

namespace Ami.Extension
{
    public class EditorPlayAudioClip
    {
        private enum PreviewStrategyType { AudioSource, DirectPlayback }
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

        public event Action OnPlaybackIndicatorUpdate;

        private EditorPreviewStrategy _currentStrategy;
        
        public PlaybackIndicatorUpdater PlaybackIndicator => _currentStrategy?.PlaybackIndicator;

        public Action OnFinished
        {
            get => _currentStrategy?.OnFinished;
            set
            {
                if (_currentStrategy != null)
                {
                    _currentStrategy.OnFinished = value;
                }
            }
        }

        private EditorPlayAudioClip()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void PlayClipByAudioSource(PreviewRequest req, bool selfLoop = false, ReplayData replayData = null)
        {
            SwitchToStrategy(PreviewStrategyType.AudioSource);
            _currentStrategy.Play(req, selfLoop, replayData);
        }

        public void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
        {
            var request = new PreviewRequest(audioClip)
            {
                StartPosition = startTime,
                EndPosition = endTime
            };

            SwitchToStrategy(PreviewStrategyType.DirectPlayback);
            _currentStrategy.Play(request, loop);
        }

        public void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
        {
            if (audioClip == null)
            {
                return;
            }

            float startTime = startSample / (float)audioClip.frequency;
            float endTime = endSample / (float)audioClip.frequency;

            PlayClip(audioClip, startTime, endTime, loop);
        }

        public void StopAllClips()
        {
            StopAndDestroyStrategy();
        }

        private void SwitchToStrategy(PreviewStrategyType strategyType)
        {
            if (_currentStrategy != null && GetCurrentStrategyType() != strategyType)
            {
                StopAndDestroyStrategy();
            }

            _currentStrategy ??= CreateStrategy(strategyType);
            _currentStrategy.AddPlaybackIndicatorListener(OnPlaybackIndicatorUpdate);
        }

        private EditorPreviewStrategy CreateStrategy(PreviewStrategyType strategyType)
        {
            return strategyType switch
            {
                PreviewStrategyType.AudioSource => new AudioSourcePreviewStrategy(),
                PreviewStrategyType.DirectPlayback => new DirectPlaybackPreviewStrategy(),
                _ => throw new ArgumentException($"Unknown strategy type: {strategyType}")
            };
        }

        private PreviewStrategyType GetCurrentStrategyType() => _currentStrategy switch
        {
            AudioSourcePreviewStrategy => PreviewStrategyType.AudioSource,
            _ => PreviewStrategyType.DirectPlayback
        };
        
        private void StopAndDestroyStrategy()
        {
            if (_currentStrategy == null)
            {
                return;
            }
            _currentStrategy.RemovePlaybackIndicatorListener(OnPlaybackIndicatorUpdate);
            _currentStrategy.Stop();
            _currentStrategy.Dispose();
            _currentStrategy = null;
        }

        private void Dispose()
        {
            OnFinished = null;
            _currentStrategy?.Dispose();
            _currentStrategy = null;
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