using System;

namespace Ami.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable, IVolumeSettable, IMusicDecoratable, IAudioStoppable, IPitchSettable
	{
		/// <summary>
		/// The SoundID of the player is playing
		/// </summary>
		SoundID ID { get; }

        /// <summary>
        /// Returns true if the player is playing
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Triggered when the audio player has finished playing
        /// </summary>
        event Action<SoundID> OnEndPlaying;

        IAudioPlayer SetVelocity(int velocity);
    }
}