using Ami.Extension;

namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
		float MasterVolume { get; }
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
        SpatialSetting SpatialSetting { get; }
		int Priority { get; }
		float Pitch { get; }
		RandomFlags RandomFlags { get; }
		float PitchRandomRange { get; }
		float VolumeRandomRange { get; }
        BroAudioClip PickNewClip();
    } 
}
