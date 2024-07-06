namespace Ami.BroAudio.Editor
{
	public enum TransportType 
	{ 
		Start = 1 << 0,
		End = 1 << 1,
		Delay = 1 << 2,
		FadeIn = 1 << 3,
		FadeOut =  1 << 4,
	}
}
