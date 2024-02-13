namespace Ami.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable,IVolumeSettable,IMusicDecoratable
	{
		int ID { get; }
		bool IsPlaying { get; }
	}
}