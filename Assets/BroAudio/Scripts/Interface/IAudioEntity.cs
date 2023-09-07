namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
		float Delay { get; }
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
		BroAudioClip Clip { get; }
	} 
}
