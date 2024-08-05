using System;

namespace Ami.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable, IVolumeSettable, IMusicDecoratable, IAudioStoppable, IAudioPlayerContent
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
        [Obsolete("Use " + nameof(OnEnd) + " instead")]
        event Action<SoundID> OnEndPlaying;

        internal IAudioPlayer SetVelocity(int velocity);
        internal IAudioPlayer SetPitch(float pitch, float fadeTime);

        IAudioPlayer OnStart(Action<IAudioPlayerContent> onStart);
        IAudioPlayer OnUpdate(Action<IAudioPlayerContent> onUpdate);
        IAudioPlayer OnEnd(Action onEnd);
        IAudioPlayer SetOnAudioFilterRead(Action<float[], int> onAudioFilterRead);
    }
}