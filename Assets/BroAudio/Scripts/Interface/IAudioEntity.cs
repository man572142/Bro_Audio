namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
		BroAudioClip Clip { get; }
        SpatialSettings SpatialSettings { get; }
    } 
}
