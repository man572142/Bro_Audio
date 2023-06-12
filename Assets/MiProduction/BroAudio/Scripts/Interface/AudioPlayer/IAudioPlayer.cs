namespace MiProduction.BroAudio
{
	public interface IAudioPlayer : IExclusiveDecoratable,IVolumeSettable,IPlaybackControlGettable,IMusicDecoratable
	{
		int ID { get; }
		bool IsPlaying { get; }
	}
}