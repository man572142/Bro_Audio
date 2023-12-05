namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
		float MasterVolume { get; }
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
		BroAudioClip Clip { get; }
        SpatialSettings SpatialSettings { get; }
		int Priority { get; }
		float Pitch { get; }
		RandomFlags RandomFlags { get; }
		float PitchRandomRange { get; }
		float VolumeRandomRange { get; }
    } 
}
