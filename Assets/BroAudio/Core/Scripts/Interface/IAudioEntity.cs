using Ami.Extension;

namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
        PlaybackGroup PlaybackGroup { get; }
        bool Loop { get; }
		bool SeamlessLoop { get; }
		float TransitionTime { get; }
        SpatialSetting SpatialSetting { get; }
		int Priority { get; }
        IBroAudioClip PickNewClip();
        IBroAudioClip PickNewClip(int context);
        float GetMasterVolume();
		float GetPitch();
		float GetRandomValue(float baseValue, RandomFlag flags);
        void ResetShuffleInUseState();
        MulticlipsPlayMode GetMulticlipsPlayMode();
    } 
}
