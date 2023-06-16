namespace MiProduction.BroAudio
{
	public static class BroAudioPlayerChain
	{
		private const float DefaultFadeTime = 0.5f;
		private const float DefaultLowPassFrequence = 300f;
		private const float DefaultHighPassFrequence = 2000f;

		public static IAudioPlayer SetVolume(this IAudioPlayer player, float vol, float fadeTime = DefaultFadeTime) 
			=> player.SetVolume(vol, fadeTime);
		public static IAudioPlayer SetVolume(this IMusicPlayer player, float vol, float fadeTime = DefaultFadeTime) 
			=> player.SetVolume(vol, fadeTime);
		public static IAudioPlayer SetVolume(this IPlayerExclusive player, float vol, float fadeTime = DefaultFadeTime) 
			=> player.SetVolume(vol, fadeTime);

		public static IPlaybackControllable GetPlaybackControl(this IMusicPlayer player)
			=> player.GetPlaybackControl();
		public static IPlaybackControllable GetPlaybackControl(this IAudioPlayer player)
			=> player.GetPlaybackControl();
		public static IPlaybackControllable GetPlaybackControl(this IPlayerExclusive player)
			=> player.GetPlaybackControl();


		public static IMusicPlayer AsMusic(this IAudioPlayer player) 
			=> player.AsMusic();
		public static IMusicPlayer AsMusic(this IPlayerExclusive player)
			=> player.AsMusic();
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition) 
			=> player.SetTransition(transition, Runtime.AudioPlayer.UseLibraryManagerSetting);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, float overrideFade)
			=> player.SetTransition(transition, default, overrideFade);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode)
			=> player.SetTransition(transition, stopMode, Runtime.AudioPlayer.UseLibraryManagerSetting);
		public static IMusicPlayer SetTransition(this IMusicPlayer player, Transition transition, StopMode stopMode, float overrideFade)
			=> player.SetTransition(transition, stopMode, overrideFade);


		public static IPlayerExclusive AsExclusive(this IAudioPlayer player) 
			=> player.AsExclusive();
		public static IPlayerExclusive AsExclusive(this IMusicPlayer player)
			=> player.AsExclusive();

		/// <summary>
		/// ���F��Player�H�~����LPlayer�����ܫ��w�����q�A���켽�񧹲�����
		/// </summary>
		/// <param name="othersVol">�ȶ�����0~1����</param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerExclusive DuckOthers(this IPlayerExclusive player, float othersVol, float fadeTime = DefaultFadeTime) 
			=> player.DuckOthers(othersVol,fadeTime);

		/// <summary>
		/// ���F��Player�H�~��Player���ϥΧC�q�ĪG���A���켽�񧹲�����
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerExclusive LowPassOthers(this IPlayerExclusive player, float freq = DefaultLowPassFrequence, float fadeTime = DefaultFadeTime) 
			=> player.LowPassOthers(freq,fadeTime);

		/// <summary>
		/// ���F��Player�H�~��Player���ϥΰ��q�ĪG���A���켽�񧹲�����
		/// </summary>
		/// <param name="freq"></param>
		/// <param name="fadeTime"></param>
		/// <returns></returns>
		public static IPlayerExclusive HighPassOthers(this IPlayerExclusive player, float freq = DefaultHighPassFrequence, float fadeTime = DefaultFadeTime) 
			=> player.HighPassOthers(freq,fadeTime);




	}

}