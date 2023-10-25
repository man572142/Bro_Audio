using System;

namespace Ami.BroAudio.Editor
{
	public interface ITransport
	{
		float StartPosition { get; }
		float EndPosition { get; }
		float Delay { get; }
		float FadeIn { get; }
		float FadeOut { get; }
		float Length { get; }
		float[] PlaybackValues { get; }
		float[] FadingValues { get; }
		void SetValue(float value, TransportType transportType);
	}
}
