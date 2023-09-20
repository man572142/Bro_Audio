using static Ami.BroAudio.Tools.BroAdvice;

namespace Ami.BroAudio
{
	public static class BroAudioChainingMethod
	{
		public static IAudioPlayer SetVolume(this IAudioPlayer player, float vol, float fadeTime = FadeTime_Immediate) 
			=> player?.SetVolume(vol, fadeTime);
		public static IAudioPlayer SetVolume(this IMusicPlayer player, float vol, float fadeTime = FadeTime_Immediate) 
			=> player?.SetVolume(vol, fadeTime);
		public static IAudioPlayer SetVolume(this IPlayerEffect player, float vol, float fadeTime = FadeTime_Immediate) 
			=> player?.SetVolume(vol, fadeTime);

		public static IMusicPlayer AsBGM(this IAudioPlayer player) 
			=> player?.AsBGM();
		public static IMusicPlayer AsBGM(this IPlayerEffect player)
			=> player?.AsBGM();
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition) 
			=> player?.SetTransition(transition, Runtime.AudioPlayer.UseLibraryManagerSetting);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, float overrideFade)
			=> player?.SetTransition(transition, default, overrideFade);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode)
			=> player?.SetTransition(transition, stopMode, Runtime.AudioPlayer.UseLibraryManagerSetting);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode, float overrideFade)
			=> player?.SetTransition(transition, stopMode, overrideFade);

#if !UNITY_WEBGL
		public static IPlayerEffect AsDominator(this IAudioPlayer player, BroAudioType dominatedType = BroAudioType.All) 
			=> player?.AsDominator(dominatedType);
		public static IPlayerEffect AsDominator(this IMusicPlayer player, BroAudioType dominatedType = BroAudioType.All)
			=> player?.AsDominator(dominatedType);

		/// <summary>
		/// 除了此Player以外的其他Player都降至指定的音量，直到播放完畢為止
		/// </summary>
		/// <param name="othersVol">值須介於0~1之間</param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerEffect QuietOthers(this IPlayerEffect player, float othersVol, float fadeTime = FadeTime_Quick) 
			=> player?.QuietOthers(othersVol,fadeTime);

		/// <summary>
		/// 除了此Player以外的Player都使用低通效果器，直到播放完畢為止
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerEffect LowPassOthers(this IPlayerEffect player, float freq = LowPassFrequence, float fadeTime = FadeTime_Quick) 
			=> player?.LowPassOthers(freq,fadeTime);

		/// <summary>
		/// 除了此Player以外的Player都使用高通效果器，直到播放完畢為止
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerEffect HighPassOthers(this IPlayerEffect player, float freq = HighPassFrequence, float fadeTime = FadeTime_Quick) 
			=> player?.HighPassOthers(freq,fadeTime);
#endif

	}

}