using System;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable,IVolumeSettable
    {
		int ID { get; }
#if UNITY_2020_2_OR_NEWER
		internal IMusicPlayer SetTransition(Transition transition, StopMode stopMode, float overrideFade);
#else
		IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
#endif
	}
}