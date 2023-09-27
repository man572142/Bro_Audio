namespace Ami.BroAudio.Editor
{
	public interface ITransport
	{
		float StartPosition { get; set; }
		float EndPosition { get; set; }
		float FadeIn { get; set; }
		float FadeOut { get; set; }
		float FullLength { get; set; }

		float[] GetMultiFloatValues(TransportType transportType);
		void ClampAndSetProperty(TransportType transportType);
	}
}
