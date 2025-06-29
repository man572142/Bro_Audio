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
		private PreviewClip _clip;
		private double _playingStartTime;
		private float _speed = 1f;
        private bool _isLoop;

        private bool _isPlaying;
        private static Color Color => new Color(1f,1f,1f,0.8f);

		protected override float UpdateInterval => 0.02f;  // 50 FPS
        
        public void Draw(Rect scope, Vector2 positionOffset = default)
        {
            if (!_isPlaying)
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

		public void SetClipInfo(Rect waveformRect, PreviewClip clip, float pitch = 1f) 
		{
			_waveformRect = waveformRect;
			_clip = clip;
			_speed = pitch;
		}

        private Rect GetIndicatorPosition()
		{
			if(_clip.FullLength != 0f && _waveformRect != default)
			{
				double currentPlayedLength = (EditorApplication.timeSinceStartup - _playingStartTime) * _speed;
				double currentPos;
				if(_isLoop)
				{
					float targetPlayLength = _clip.FullLength - _clip.StartPosition - _clip.EndPosition;
                    currentPos = _clip.StartPosition + currentPlayedLength % targetPlayLength;
				}
                else
                {
					float endTime = _clip.FullLength - _clip.EndPosition;
					currentPos = _clip.StartPosition + currentPlayedLength;
                    currentPos = Math.Min(currentPos, endTime);
				}
                
				float x = (float)(_waveformRect.x + (currentPos / _clip.FullLength * _waveformRect.width));
				return new Rect(x,_waveformRect.y, AudioClipIndicatorWidth,_waveformRect.height);
			}
			return default;
		}

        public override void Start()
		{
			_playingStartTime = EditorApplication.timeSinceStartup;
			_isPlaying = true;
			_isLoop = false;
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
			_playingStartTime = default;
			base.End();
		}

        public override void Dispose()
        {
			OnEnd = null;
            base.Dispose();
        }
    }
}