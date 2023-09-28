using System;

namespace Ami.BroAudio.Editor
{
	public interface ITransport
	{
		event Action<TransportType> OnTransportChanged; 

		float StartPosition { get; set; }
		float EndPosition { get; set; }
		float FadeIn { get; set; }
		float FadeOut { get; set; }
		float FullLength { get; set; }

		float[] GetMultiFloatValues(TransportType transportType);
	}

	public interface IReadOnlyTransport
	{
		float StartPosition { get; }
		float EndPosition { get;}
		float FadeIn { get;}
		float FadeOut { get;}
		float FullLength { get; }
	}
}
