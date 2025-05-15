using System;
using Ami.BroAudio.Editor;
using UnityEngine;

namespace Ami.Extension
{
    public class PlaybackIndicatorUpdater : EditorUpdateHelper
    {
        public const float AudioClipIndicatorWidth = 2f;

        public event Action OnEnd;

        private Rect _waveformRect = default;
        private PreviewClipInfo _info = default;
        private float _speed = 1f;
        private bool _isVisible = false;
        private float _playbackPosition = 0f;
        
        public bool IsPlaying { get; private set; }
        public bool IsLoop { get; private set; }

        public Color Color => new Color(1f,1f,1f,0.8f);

        protected override float UpdateInterval => 0.02f;  // 50 FPS

        public void SetClipInfo(Rect waveformRect, PreviewClipInfo info, float pitch) 
        {
            _waveformRect = waveformRect;
            _info = info;
            _speed = pitch;
        }

        public Rect GetIndicatorPosition()
        {
            if(_isVisible && _info.FullLength != 0f && _waveformRect != default)
            {
                double currentPos;
                if(IsLoop)
                {
                    float targetPlayLength = _info.FullLength - _info.StartPosition - _info.EndPosition;
                    currentPos = _info.StartPosition + _playbackPosition % targetPlayLength;
                }
                else
                {
                    float endTime = _info.FullLength - _info.EndPosition;
                    currentPos = _info.StartPosition + _playbackPosition;
                    currentPos = Math.Min(currentPos, endTime);
                }
                
                float x = (float)(_waveformRect.x + (currentPos / _info.FullLength * _waveformRect.width));
                return new Rect(x,_waveformRect.y, AudioClipIndicatorWidth,_waveformRect.height);
            }
            return default;
        }

        public void SetVisible(bool isVisible)
        {
            _isVisible = isVisible;
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
        }

        public override void Start()
        {
            _isVisible = true;
            IsPlaying = true;
            IsLoop = false;
            _playbackPosition = 0f;
            base.Start();
        }

        public void Start(bool loop)
        {
            Start();
            IsLoop = loop;
        }

        public override void End()
        {
            if(IsPlaying)
            {
                IsPlaying = false;
                OnEnd?.Invoke();
            }
            _isVisible = false;
            base.End();
        }

        public override void Dispose()
        {
            OnEnd = null;
            base.Dispose();
        }

        protected override void Update()
        {
            base.Update();
            _playbackPosition += DeltaTime * _speed;
        }
    }
}