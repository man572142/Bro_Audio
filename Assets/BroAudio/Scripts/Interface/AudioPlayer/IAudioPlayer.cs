namespace MiProduction.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable,IVolumeSettable,IPlaybackControlGettable,IMusicDecoratable
	{
		int ID { get; }
		bool IsPlaying { get; }
	}
}