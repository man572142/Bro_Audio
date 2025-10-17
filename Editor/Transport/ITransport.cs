using System;

namespace Ami.BroAudio.Editor
{
	public interface ITransport : IClip
	{
		float Delay { get; }
		float FadeIn { get; }
		float FadeOut { get; }
		float[] PlaybackValues { get; }
		float[] FadingValues { get; }
		void SetValue(float value, TransportType transportType);
		void Update();
	}

	public interface IClip
	{
		float StartPosition { get; }
		float EndPosition { get; }
		float FullLength { get; }
	}
}
