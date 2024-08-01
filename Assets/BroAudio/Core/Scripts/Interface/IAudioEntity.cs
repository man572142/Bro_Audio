using Ami.Extension;

namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
        SpatialSetting SpatialSetting { get; }
		int Priority { get; }
        BroAudioClip PickNewClip();
        BroAudioClip PickNewClip(int velocity);
        float GetMasterVolume();
		float GetPitch();
		float GetRandomValue(float baseValue, RandomFlag flags);
        void ResetShuffleInUseState();
    } 
}
