using System;
using Ami.BroAudio.Editor;
using UnityEditor;
using UnityEngine;

namespace Ami.Extension
{
	public class PlaybackIndicatorUpdater : EditorUpdateHelper
	{
        private const float AudioClipIndicatorWidth = 2f;

		public event Action OnEnd;

		private Rect _waveformRect;
		private PreviewRequest _request;
        private bool _isLoop;
        private bool _isVisible;
        private float _playbackPosition;

        private bool _isPlaying;
        private static Color Color => new Color(1f,1f,1f,0.8f);

		protected override float UpdateInterval => 0.02f;  // 50 FPS
        
        public void Draw(Rect scope, Vector2 positionOffset = default)
        {
            if (!_isPlaying || !_isVisible)
            {
                return;
            }

            GUI.BeginClip(scope);
            {
                Rect indicatorRect = GetIndicatorPosition();
                EditorGUI.DrawRect(new Rect(indicatorRect.position + positionOffset, indicatorRect.size), Color);
            }
            GUI.EndClip();
        }

		public void SetClipInfo(Rect waveformRect, PreviewRequest req) 
		{
			_waveformRect = waveformRect;
			_request = req;
		}

        private Rect GetIndicatorPosition()
        {
            var fullLength = _request?.PreciseAudioClipLength ?? 0f;
            if (fullLength <= 0f || _waveformRect == default)
            {
                return default;
            }
            
            double currentPos;
            if(_isLoop)
            {
                var targetPlayLength = fullLength - _request.StartPosition - _request.EndPosition;
                currentPos = _request.StartPosition + _playbackPosition % targetPlayLength;
            }
            else
            {
                var endTime = fullLength - _request.EndPosition;
                currentPos = _request.StartPosition + _playbackPosition;
                currentPos = Math.Min(currentPos, endTime);
            }
                
            float x = (float)(_waveformRect.x + (currentPos / fullLength * _waveformRect.width));
            return new Rect(x,_waveformRect.y, AudioClipIndicatorWidth,_waveformRect.height);
        }
        
        public void SetVisibility(bool isVisible)
        {
            _isVisible = isVisible;
        }

        public override void Start()
		{
			_isPlaying = true;
            _isVisible = true;
			_isLoop = false;
            _playbackPosition = 0f;
			base.Start();
		}

		public void Start(bool loop)
		{
			Start();
			_isLoop = loop;
		}

		public override void End()
		{
			if(_isPlaying)
			{
				_isPlaying = false;
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
            if (_request != null)
            {
                _playbackPosition += DeltaTime * _request.Pitch;
            }
        }
    }
}