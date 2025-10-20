namespace Ami.BroAudio.Runtime
{
    public abstract class AudioPlayerDecorator : AudioPlayerInstanceWrapper
    {
        protected AudioPlayerDecorator(AudioPlayer instance) : base(instance)
        {
        }
    }
}