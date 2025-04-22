using System;
using Ami.BroAudio.Editor;
using UnityEditor;
using UnityEngine;

namespace Ami.Extension
{
	public class PlaybackIndicatorUpdater : EditorUpdateHelper
	{
		public const float AudioClipIndicatorWidth = 2f;

		public event Action OnEnd;

		private Rect _waveformRect = default;
		private PreviewClipInfo _info = default;
		private double _playingStartTime = default;
		private float _speed = 1f;
        private bool _isVisible = false;
		
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
				double currentPlayedLength = (EditorApplication.timeSinceStartup - _playingStartTime) * _speed;
				double currentPos;
				if(IsLoop)
				{
					float targetPlayLength = _info.FullLength - _info.StartPosition - _info.EndPosition;
                    currentPos = _info.StartPosition + currentPlayedLength % targetPlayLength;
				}
                else
                {
					float endTime = _info.FullLength - _info.EndPosition;
					currentPos = _info.StartPosition + currentPlayedLength;
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

		public override void Start()
		{
            _isVisible = true;
            _playingStartTime = EditorApplication.timeSinceStartup;
			IsPlaying = true;
			IsLoop = false;
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
			_playingStartTime = default;
            _isVisible = false;
			base.End();
		}

        public override void Dispose()
        {
			OnEnd = null;
            base.Dispose();
        }
    }
}