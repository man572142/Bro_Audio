using System;
using MiProduction.BroAudio.Runtime;

namespace MiProduction.BroAudio
{
    public interface IMusicPlayer : IExclusiveDecoratable,IVolumeSettable,IPlaybackControlGettable
    {
		public int ID { get; }
		internal IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
	}
}