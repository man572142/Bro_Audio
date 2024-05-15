using System;

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

        void Stop();
        void Stop(float fadeOut);
        void Stop(float fadeOut, Action onFinished);
        void Pause();
        void Pause(float fadeOut);
    }
}