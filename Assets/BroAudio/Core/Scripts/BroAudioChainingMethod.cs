﻿using static Ami.BroAudio.BroAdvice;

namespace Ami.BroAudio
{
	public static class BroAudioChainingMethod
	{
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
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition) 
			=> player?.SetTransition(transition, Runtime.AudioPlayer.UseEntitySetting);

		/// <param name="overrideFade">Override value of the fading time</param>
		/// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, float overrideFade)
			=> player?.SetTransition(transition, default, overrideFade);

		/// <param name="stopMode">The stop mode of the previous player</param>
		/// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode)
			=> player?.SetTransition(transition, stopMode, Runtime.AudioPlayer.UseEntitySetting);

		/// <param name="overrideFade">Override value of the fading time</param>
		/// <inheritdoc cref="SetTransition(IMusicPlayer, Transition,StopMode)"/>
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode, float overrideFade)
			=> player?.SetTransition(transition, stopMode, overrideFade);
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