namespace Ami.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable,IVolumeSettable,IMusicDecoratable
	{
		/// <summary>
		/// The SoundID of the player is playing
		/// </summary>
		int ID { get; }

        /// <summary>
        /// Returns true if the player is playing
        /// </summary>
        bool IsPlaying { get; }
	}
}