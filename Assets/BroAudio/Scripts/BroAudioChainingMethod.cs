using static Ami.BroAudio.Tools.BroAdvice;

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
			=> player?.SetTransition(transition, Runtime.AudioPlayer.UseLibraryManagerSetting);

		/// <param name="overrideFade">Override value of the fading time</param>
		/// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, float overrideFade)
			=> player?.SetTransition(transition, default, overrideFade);

		/// <param name="stopMode">The stop mode of the previous player</param>
		/// <inheritdoc cref="SetTransition(IMusicPlayer, Transition)"/>
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode)
			=> player?.SetTransition(transition, stopMode, Runtime.AudioPlayer.UseLibraryManagerSetting);

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
		/// <param name="dominatedType"></param>
		public static IPlayerEffect AsDominator(this IAudioPlayer player, BroAudioType dominatedType = BroAudioType.All) 
			=> player?.AsDominator(dominatedType);

		/// <inheritdoc cref="AsDominator(IAudioPlayer, BroAudioType)"/>
		public static IPlayerEffect AsDominator(this IMusicPlayer player, BroAudioType dominatedType = BroAudioType.All)
			=> player?.AsDominator(dominatedType);

		/// <summary>
		/// While this audio player is playing, the volume of other audio players will be lowered to the given ratio.
		/// </summary>
		/// <param name="othersVol">值須介於0~1之間</param>
		/// <param name="fadeTime"></param>
		public static IPlayerEffect QuietOthers(this IPlayerEffect player, float othersVol, float fadeTime = FadeTime_Quick) 
			=> player?.QuietOthers(othersVol,fadeTime);

		/// <summary>
		/// While this audio player is playing, the higher frequencies of other audio players will be cutted off. (aka LowPassFilter) 
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		public static IPlayerEffect HighCutOthers(this IPlayerEffect player, float freq = HighCutFrequence, float fadeTime = FadeTime_Quick) 
			=> player?.HighCutOthers(freq,fadeTime);

		/// <summary>
		/// While this audio player is playing, the lower frequencies of other audio players will be cutted off. (aka HighPassFilter) 
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		public static IPlayerEffect LowCutOthers(this IPlayerEffect player, float freq = LowCutFrequence, float fadeTime = FadeTime_Quick) 
			=> player?.LowCutOthers(freq,fadeTime);
		#endregion
#endif
	}
}