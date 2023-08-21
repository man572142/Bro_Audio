using System;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable,IVolumeSettable,IPlaybackControlGettable
    {
		public int ID { get; }
		public bool IsPlaying { get; }
		IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
	}
}