using System;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable,IVolumeSettable,IPlaybackControlGettable
    {
		public int ID { get; }
		internal IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
	}
}