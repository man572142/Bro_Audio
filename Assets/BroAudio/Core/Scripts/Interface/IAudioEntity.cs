using Ami.BroAudio.Runtime;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
	public interface IAudioEntity
	{
        PlaybackGroup PlaybackGroup { get; }
        SpatialSetting SpatialSetting { get; }
        bool HasLoop(out LoopType loopType, out float transitionTime);
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
