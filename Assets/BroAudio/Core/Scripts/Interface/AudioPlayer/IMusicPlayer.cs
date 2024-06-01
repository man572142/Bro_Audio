namespace Ami.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable, IVolumeSettable, IAudioStoppable
    {
		SoundID ID { get; }
#if UNITY_2020_2_OR_NEWER
		internal IMusicPlayer SetTransition(Transition transition, StopMode stopMode, float overrideFade);
#else
		IMusicPlayer SetTransition(Transition transition,StopMode stopMode,float overrideFade);
#endif
	}
}