using System;
using Ami.BroAudio.Editor;
using UnityEditor;
using UnityEngine;

namespace Ami.Extension
{
	public class PlaybackIndicatorUpdater : EditorUpdateHelper
	{
		public const float AudioClipIndicatorMotionInterval = 0.02f;
		public const float AudioClipIndicatorWidth = 2f;

		public event Action OnUpdate;

		private Rect _waveformRect = default;
		private ITransport _transport = null;
		private double _startTime = default;
		
		public bool IsPlaying { get; private set; }

		public Color Color => new Color(1f,1f,1f,0.8f);
		public PlaybackIndicatorUpdater() : base(AudioClipIndicatorMotionInterval)
		{
		}

		public void SetClipInfo(Rect waveformRect, ITransport transport)
		{
			_waveformRect = waveformRect;
			_transport = transport;
		}

		public Rect GetIndicatorPosition()
		{
			if(_transport != null && _waveformRect != default)
			{
				double currentTime = _transport.StartPosition + (EditorApplication.timeSinceStartup - _startTime);
				float x = _waveformRect.x + ((float)currentTime / _transport.Length * _waveformRect.width);
				return new Rect(x,_waveformRect.y, AudioClipIndicatorWidth,_waveformRect.height);
			}
			return default;
		}

		public override void Start()
		{
			_startTime = EditorApplication.timeSinceStartup;
			IsPlaying = true;
			base.Start();
		}

		public override void End()
		{
			base.End();
			IsPlaying = false;
		}

		protected override void Update()
		{
			OnUpdate?.Invoke();
		}
	}
}