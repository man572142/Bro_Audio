namespace Ami.BroAudio
{
    public interface IMusicPlayer : IEffectDecoratable, IVolumeSettable, IAudioStoppable
    {
        SoundID ID { get; }
        internal IMusicPlayer SetTransition(Transition transition, StopMode stopMode, float overrideFade);
    }
}