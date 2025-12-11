using System;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio
{
    public interface IAudioPlayer : IEffectDecoratable, IVolumeSettable, IMusicDecoratable, IAudioStoppable, ISchedulable
    {
        /// <summary>
        /// The SoundID of the player is playing
        /// </summary>
        SoundID ID { get; }

        /// <summary>  
        /// Indicates whether the player is currently in the playback process, including queued, playing, or paused states.
        /// </summary>  
        bool IsActive { get; }

        /// <summary>  
        /// Returns true if the player is playing
        /// </summary>  
        bool IsPlaying { get; }

        Data.IBroAudioClip CurrentPlayingClip { get; }

        internal IAudioPlayer SetVelocity(int velocity);
        internal IAudioPlayer SetPitch(float pitch, float fadeTime);

        /// <summary>
        /// Triggered when the AudioPlayer starts to play
        /// </summary>
        IAudioPlayer OnStart(Action<IAudioPlayer> onStart);

        /// <summary>
        /// Triggered each frame while the AudioPlayer is playing
        /// </summary>
        IAudioPlayer OnUpdate(Action<IAudioPlayer> onUpdate);

        /// <summary>
        /// Triggered when the AudioPlayer is paused
        /// </summary>
        IAudioPlayer OnPause(Action<IAudioPlayer> onPause);

        /// <summary>
        /// Triggered when the AudioPlayer stops playing
        /// </summary>
        IAudioPlayer OnEnd(Action<SoundID> onEnd);
        
        /// <summary>
        /// Sets the fade in easing function for this player
        /// </summary>
        IAudioPlayer SetFadeInEase(Ease ease);
        
        /// <summary>
        /// Sets the fade out easing function for this player
        /// </summary>
        IAudioPlayer SetFadeOutEase(Ease ease);

        internal IAudioPlayer OnAudioFilterRead(Action<float[], int> onAudioFilterRead);

        IAudioSourceProxy AudioSource { get; }

        /// <inheritdoc cref="AudioSource.GetOutputData(float[], int)"/>
        void GetOutputData(float[] samples, int channels);

        /// <inheritdoc cref="AudioSource.GetSpectrumData(float[], int, FFTWindow)"/>
        void GetSpectrumData(float[] samples, int channels, FFTWindow window);

        /// <summary>
        /// Adds an audio effect component to the audio player
        /// </summary>
        /// <param name="onSet">Optional callback to configure the component after creation</param>
        internal IAudioPlayer AddAudioEffect<T, TProxy>(Action<TProxy> onSet)
            where T : Behaviour 
            where TProxy : class;

        /// <summary>
        /// Removes a specified audio effect component from the audio player
        /// </summary>
        internal IAudioPlayer RemoveAudioEffect<T>()
            where T : Behaviour;
    }
}