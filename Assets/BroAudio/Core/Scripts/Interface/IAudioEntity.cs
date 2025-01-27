using Ami.Extension;

namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
        PlaybackGroup Group { get; set; }
		bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
        SpatialSetting SpatialSetting { get; }
		int Priority { get; }
        IBroAudioClip PickNewClip();
        IBroAudioClip PickNewClip(int velocity);
        float GetMasterVolume();
		float GetPitch();
		float GetRandomValue(float baseValue, RandomFlag flags);
        void ResetShuffleInUseState();
    } 
}
