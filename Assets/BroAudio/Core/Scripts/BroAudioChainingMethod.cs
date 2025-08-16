using System;
using Ami.Extension;
using UnityEngine;
using static Ami.BroAudio.BroAdvice;

namespace Ami.BroAudio
{
    public static class BroAudioChainingMethod
    {
        #region Stop
        public static void Stop(this IAudioStoppable player)
    => player?.Stop();
        public static void Stop(this IAudioStoppable player, Action onFinished)
            => player?.Stop(onFinished);

        public static void Stop(this IAudioStoppable player, float fadeOut)
            => player?.Stop(fadeOut);

        public static void Stop(this IAudioStoppable player, float fadeOut, Action onFinished)
            => player?.Stop(fadeOut, onFinished);

        public static void Pause(this IAudioStoppable player)
            => player?.Pause();

        public static void Pause(this IAudioStoppable player, float fadeOut)
            => player?.Pause(fadeOut);

        public static void UnPause(this IAudioStoppable player)
            => player?.UnPause();

        public static void UnPause(this IAudioStoppable player, float fadeOut)
            => player?.UnPause( fadeOut);
        #endregion

        #region Set Volume
        /// <summary>
        /// Set the player's volume (0~1)
        /// </summary>
        /// <param name="vol">The target volume</param>
        /// <param name="fadeTime">Time to reach the target volume from the current volume.</param>
        public static IAudioPlayer SetVolume(this IAudioPlayer player, float vol, float fadeTime = FadeTime_Immediate) 
            => player?.SetVolume(vol, fadeTime);

        /// <inheritdoc cref="SetVolume(IAudioPlayer,float,float)"/>
        public static IAudioPlayer SetVolume(this IMusicPlayer player, float vol, float fadeTime = FadeTime_Immediate) 
            => player?.SetVolume(vol, fadeTime);

        /// <inheritdoc cref="SetVolume(IAudioPlayer,float,float)"/>
        public static IAudioPlayer SetVolume(this IPlayerEffect player, float vol, float fadeTime = FadeTime_Immediate) 
            => player?.SetVolume(vol, fadeTime);
        #endregion

        #region Set Pitch
        /// <summary>
        /// Set the player's pitch.
        /// </summary>
        /// <param name="pitch">Values between -3 to 3, default is 1</param>
        /// <param name="fadeTime">Time to reach the target volume from the current volume.</param>
        public static IAudioPlayer SetPitch(this IAudioPlayer player, float pitch, float fadeTime = FadeTime_Immediate)
            => player?.SetPitch(pitch, fadeTime);

        /// <inheritdoc cref="SetPitch(IAudioPlayer,float,float)"/>
        public static IAudioPlayer SetPitch(this IMusicPlayer player, float pitch, float fadeTime = FadeTime_Immediate)
            => player?.SetPitch(pitch, fadeTime);

        /// <inheritdoc cref="SetPitch(IAudioPlayer,float,float)"/>
        public static IAudioPlayer SetPitch(this IPlayerEffect player, float pitch, float fadeTime = FadeTime_Immediate)
            => player?.SetPitch(pitch, fadeTime);
        #endregion

        /// <inheritdoc cref="ISchedulable.SetScheduledStartTime(double)"/>
        public static IAudioPlayer SetScheduledStartTime(this IAudioPlayer player, double dspTime)
            => player?.SetScheduledStartTime(dspTime);

        /// <inheritdoc cref="ISchedulable.SetScheduledEndTime(double)"/>
        public static IAudioPlayer SetScheduledEndTime(this IAudioPlayer player, double dspTime)
            => player?.SetScheduledEndTime(dspTime);

        /// <inheritdoc cref="ISchedulable.SetDelay(float)(double)"/>
        public static IAudioPlayer SetDelay(this IAudioPlayer player, float time)
            => player?.SetDelay(time);

        #region Velocity
        /// <summary>
        /// Set the velocity and use it to determine which audio clip to play
        /// </summary>
        public static IAudioPlayer SetVelocity(this IAudioPlayer player, int velocity)
            => player?.SetVelocity(velocity); 
        #endregion

        #region As Background Music
        /// <summary>
        /// As a background music, which will transition automatically if another BGM is played after it.
        /// </summary>
        public static IMusicPlayer AsBGM(this IAudioPlayer player) 
            => player?.AsBGM();

        /// <inheritdoc cref="AsBGM(IAudioPlayer)"/>
        public static IMusicPlayer AsBGM(this IPlayerEffect player)
            => player?.AsBGM();
        #endregion

        #region Set Transition
        /// <summary>
        /// Set the transition type for BGM
        /// </summary>
        /// <param name="player"></param>
        /// <param name="transition">Transition type</param>
        /// <returns></returns>
        public static IAudioPlayer SetTransition(this IMusicPlayer player, Transition transition) 
            => player?.SetTransition(transition, Runtime.AudioPlayer.UseEntitySetting);

        /// <param name="overrideFade">Override value of the fading time</param>
        /// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
        public static IAudioPlayer SetTransition(this IMusicPlayer player, Transition transition, float overrideFade)
            => player?.SetTransition(transition, default, overrideFade);

        /// <param name="stopMode">The stop mode of the previous player</param>
        /// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
        public static IAudioPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode)
            => player?.SetTransition(transition, stopMode, Runtime.AudioPlayer.UseEntitySetting);

        /// <param name="overrideFade">Override value of the fading time</param>
        /// <inheritdoc cref="SetTransition(IMusicPlayer, Transition,StopMode)"/>
        public static IAudioPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode, float overrideFade)
            => player?.SetTransition(transition, stopMode, overrideFade);
        #endregion

        public static IAudioPlayer OnAudioFilterRead(this IAudioPlayer player, Action<float[], int> onAudioFilterRead)
        {
            return player?.OnAudioFilterRead(onAudioFilterRead);
        }

        #region Set Effect
        /// <summary>
        /// Adds a chorus effect to the audio player, creating multiple delayed copies of the sound to produce a richer, fuller tone
        /// </summary>
        /// <param name="onSet">Optional callback to configure the chorus settings</param>
        public static IAudioPlayer AddChorusEffect(this IAudioPlayer player, Action<IAudioChorusFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioChorusFilter, IAudioChorusFilterProxy>(onSet);
        
        /// <summary>
        /// Adds a distortion effect to the audio player, creating audio clipping and harmonic distortion
        /// </summary>
        /// <param name="onSet">Optional callback to configure the distortion settings</param>
        public static IAudioPlayer AddDistortionEffect(this IAudioPlayer player, Action<IAudioDistortionFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioDistortionFilter, IAudioDistortionFilterProxy>(onSet);
        
        /// <summary>
        /// Adds an echo effect to the audio player, creating delayed repetitions of the original sound
        /// </summary>
        /// <param name="onSet">Optional callback to configure the echo settings</param>
        public static IAudioPlayer AddEchoEffect(this IAudioPlayer player, Action<IAudioEchoFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioEchoFilter, IAudioEchoFilterProxy>(onSet);
        
        /// <summary>
        /// Adds a high-pass filter effect to the audio player, attenuating low frequencies below the cutoff point
        /// </summary>
        /// <param name="onSet">Optional callback to configure the high-pass filter settings</param>
        public static IAudioPlayer AddHighPassEffect(this IAudioPlayer player, Action<IAudioHighPassFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioHighPassFilter, IAudioHighPassFilterProxy>(onSet);

        /// <summary>
        /// Adds a low-pass filter effect to the audio player, attenuating high frequencies above the cutoff point
        /// </summary>
        /// <param name="onSet">Optional callback to configure the low-pass filter settings</param>
        public static IAudioPlayer AddLowPassEffect(this IAudioPlayer player, Action<IAudioLowPassFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioLowPassFilter, IAudioLowPassFilterProxy>(onSet);
        
        /// <summary>
        /// Adds a reverb effect to the audio player, simulating acoustic reflections in various environments
        /// </summary>
        /// <param name="onSet">Optional callback to configure the reverb settings</param>
        public static IAudioPlayer AddReverbEffect(this IAudioPlayer player, Action<IAudioReverbFilterProxy> onSet = null) 
            => player?.AddAudioEffect<AudioReverbFilter, IAudioReverbFilterProxy>(onSet);

        /// <summary>
        /// Removes a chorus effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveChorusEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioChorusFilter>();
        
        /// <summary>
        /// Removes a distortion effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveDistortionEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioDistortionFilter>();
        
        /// <summary>
        /// Removes an echo effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveEchoEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioEchoFilter>();
        
        /// <summary>
        /// Removes a high-pass filter effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveHighPassEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioHighPassFilter>();

        /// <summary>
        /// Removes a low-pass filter effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveLowPassEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioLowPassFilter>();
        
        /// <summary>
        /// Removes a reverb effect from the audio player
        /// </summary>
        public static IAudioPlayer RemoveReverbEffect(this IAudioPlayer player) 
            => player?.RemoveAudioEffect<AudioReverbFilter>();
        #endregion

#if !UNITY_WEBGL
        #region As Dominator
        /// <summary>
        /// To be a dominator, which will affect or change the behavior of other audio players.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="dominatedType">The audio type that being dominated</param>
        public static IPlayerEffect AsDominator(this IAudioPlayer player) 
            => player?.AsDominator();

        /// <inheritdoc cref="AsDominator(IAudioPlayer, BroAudioType)"/>
        public static IPlayerEffect AsDominator(this IMusicPlayer player)
            => player?.AsDominator();

        /// <summary>
        /// While this audio player is playing, the volume of other audio players will be lowered to the given ratio.
        /// </summary>
        /// <param name="othersVol"></param>
        /// <param name="fadeTime">The time duration of the FadeIn and FadeOut</param>
        public static IPlayerEffect QuietOthers(this IPlayerEffect player, float othersVol, float fadeTime = FadeTime_Quick) 
            => player?.QuietOthers(othersVol, fadeTime);

        /// <inheritdoc cref = "QuietOthers(IPlayerEffect, float, float)" />
        /// <param name="fading">The fading setting of this action</param>
        public static IPlayerEffect QuietOthers(this IPlayerEffect player, float othersVol, Fading fading)
            => player?.QuietOthers(othersVol, fading);

        /// <summary>
        /// While this audio player is playing, a lowpass filter will be added to other audio players (i.e. their higher frequencies will be cutted off)
        /// </summary>
        /// <param name="freq">10 Hz ~ 22000Hz</param>
        /// <param name="fadeTime">The time duration of the FadeIn and FadeOut</param>
        public static IPlayerEffect LowPassOthers(this IPlayerEffect player, float freq = LowPassFrequency, float fadeTime = FadeTime_Quick) 
            => player?.LowPassOthers(freq,fadeTime);

        /// <inheritdoc cref = "LowPassOthers(IPlayerEffect, float, float)" />
        /// <param name="fading">The fading setting of this action</param>
        public static IPlayerEffect LowPassOthers(this IPlayerEffect player, float freq, Fading fading)
            => player?.LowPassOthers(freq, fading);

        /// <summary>
        /// While this audio player is playing, a highpass filter will be added to other audio players (i.e. their lower frequencies will be cutted off)
        /// </summary>
        /// <param name="freq">10 Hz ~ 22000Hz</param>
        /// <param name="fadeTime">The time duration of the FadeIn and FadeOut</param>
        public static IPlayerEffect HighPassOthers(this IPlayerEffect player, float freq = HighPassFrequency, float fadeTime = FadeTime_Quick) 
            => player?.HighPassOthers(freq,fadeTime);

        /// <inheritdoc cref = "HighPassOthers(IPlayerEffect, float, float)" />
        /// <param name="fading">The fading setting of this action</param>
        public static IPlayerEffect HighPassOthers(this IPlayerEffect player, float freq, Fading fading)
            => player?.HighPassOthers(freq, fading);

        // Note: LowPass == HighCut , HighPass == LowCut
        #endregion
#endif
    }
}