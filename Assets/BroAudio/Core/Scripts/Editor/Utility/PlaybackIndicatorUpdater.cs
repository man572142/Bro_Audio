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
		private IClip _clip = null;
		private double _playingStartTime = default;
		
		public bool IsPlaying { get; private set; }

		public Color Color => new Color(1f,1f,1f,0.8f);

		protected override float UpdateInterval => 0.02f;  // 50 FPS

		public void SetClipInfo(Rect waveformRect, IClip clip)
		{
			_waveformRect = waveformRect;
			_clip = clip;
		}

		public Rect GetIndicatorPosition()
		{
			if(_clip != null && _waveformRect != default)
			{
				float endTime = _clip.Length - _clip.EndPosition;
				double currentTime = _clip.StartPosition + (EditorApplication.timeSinceStartup - _playingStartTime);
				currentTime = currentTime > endTime ? endTime : currentTime;
				float x = _waveformRect.x + ((float)currentTime / _clip.Length * _waveformRect.width);
				return new Rect(x,_waveformRect.y, AudioClipIndicatorWidth,_waveformRect.height);
			}
			return default;
		}

		public Rect GetEndPos()
		{
			float endTime = _clip.Length - _clip.EndPosition;
			return new Rect(_waveformRect.x + (endTime / _clip.Length * _waveformRect.width), _waveformRect.y, AudioClipIndicatorWidth, _waveformRect.height);
		}

		public override void Start()
		{
			_playingStartTime = EditorApplication.timeSinceStartup;
			IsPlaying = true;
			base.Start();
		}

		public override void End()
		{
			if(IsPlaying)
			{
				IsPlaying = false;
				OnEnd?.Invoke();
			}
			_playingStartTime = default;
			base.End();
		}
	}
}