namespace Ami.BroAudio
{
	public enum StopMode
	{
		/// <summary>
		/// Stop the current playback, and it will restart if played again.
		/// </summary>
		Stop,

		/// <summary>
		/// Pause the current playback, and it will resume from where it was paused if played again.
		/// </summary>
		Pause,

		/// <summary>
		/// Mute the current playback, it will keep playing in the background until it's played(Unmuted) again. 
		/// </summary>
		Mute,
	} 
}