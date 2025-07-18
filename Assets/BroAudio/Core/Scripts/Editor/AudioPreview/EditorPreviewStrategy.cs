using Ami.Extension;
using System;
using System.Threading;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public abstract class EditorPreviewStrategy
    {
        private CancellationTokenSource _cancellationSource = new();
        private PlaybackIndicatorUpdater _playbackIndicator = new();

        protected CancellationTokenSource CancellationSource => _cancellationSource ??= new CancellationTokenSource();

        public Action OnFinished;
        public PlaybackIndicatorUpdater PlaybackIndicator => _playbackIndicator;

        public abstract void Play(PreviewRequest request, ReplayRequest replayRequest = null);
        public abstract void Stop();

        public virtual void UpdatePreview()
        {
            
        }

        protected void TriggerOnFinished()
        {
            OnFinished?.Invoke();
            OnFinished = null;
        }

        protected void CancelTask()
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
            if (_playbackIndicator != null)
            {
                _playbackIndicator.OnUpdate += action;
                _playbackIndicator.OnEnd += action;
            }
        }

        public void RemovePlaybackIndicatorListener(Action action)
        {
            if (_playbackIndicator != null)
            {
                _playbackIndicator.OnUpdate -= action;
                _playbackIndicator.OnEnd -= action;
            }
        }

        public virtual void Dispose()
        {
            OnFinished = null;
            CancelTask();
            _playbackIndicator?.Dispose();
            _playbackIndicator = null;
        }

        protected void StartPlaybackIndicator(bool loop = false)
        {
            _playbackIndicator?.Start(loop);
        }

        protected void EndPlaybackIndicator()
        {
            _playbackIndicator?.End();
        }
    }
}