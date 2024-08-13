using System;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio
{
	public interface IAudioPlayer : IEffectDecoratable, IVolumeSettable, IMusicDecoratable, IAudioStoppable
    {
		/// <summary>
		/// The SoundID of the player is playing
		/// </summary>
		SoundID ID { get; }

        /// <summary>
        /// Returns true if the player is about to play or is playing 
        /// </summary>
        bool IsActive { get; }

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

        IAudioPlayer OnStart(Action<IAudioPlayer> onStart);
        IAudioPlayer OnUpdate(Action<IAudioPlayer> onUpdate);
        IAudioPlayer OnEnd(Action<SoundID> onEnd);
        internal IAudioPlayer OnAudioFilterRead(Action<float[], int> onAudioFilterRead);

        IAudioSourceProxy AudioSource { get; }

        /// <inheritdoc cref="AudioSource.GetOutputData(float[], int)"/>
        void GetOutputData(float[] samples, int channels);

        /// <inheritdoc cref="AudioSource.GetSpectrumData(float[], int, FFTWindow)"/>
        void GetSpectrumData(float[] samples, int channels, FFTWindow window);

        /// <inheritdoc cref="AudioSource.GetSpatializerFloat(int, out float)"/>
        bool GetSpatializerFloat(int index, out float value);

        /// <inheritdoc cref="AudioSource.GetAmbisonicDecoderFloat(int, out float)"/>
        bool GetAmbisonicDecoderFloat(int index, out float value);
    }
}