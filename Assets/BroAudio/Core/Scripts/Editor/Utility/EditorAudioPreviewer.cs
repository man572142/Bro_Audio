using UnityEditor;
using System;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
    // HISTORY NOTE:
    // This class used to be called EditorPlayAudioClip.
    // The original EditorAudioPreviewer has been moved to EditorVolumeTransporter.
    public class EditorAudioPreviewer
    {
        public const string IgnoreSettingTooltip = "Right-click to play the audio clip directly";

        private static EditorAudioPreviewer _instance = null;
        public static EditorAudioPreviewer Instance
        {
            get
            {
                _instance ??= new EditorAudioPreviewer();
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

        private EditorAudioPreviewer()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void Play(PreviewRequest req, bool selfLoop = false, ReplayData replayData = null)
        {
            SwitchToStrategy(req.StrategyType);
            _currentStrategy.Play(req, selfLoop, replayData);
        }

        public void StopAllClips()
        {
            StopAndDestroyStrategy();
        }
        
        public void UpdatePreview()
        {
            _currentStrategy?.UpdatePreview();
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