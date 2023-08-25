using System;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable,IVolumeSettable,IPlaybackControlGettable
    {
		int ID { get; }
		bool IsPlaying { get; }
		IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
	}
}