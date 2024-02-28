using System;
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
	public class MusicPlayer : AudioPlayerDecorator , IMusicPlayer
	{
		public static AudioPlayer CurrentPlayer = null;

		private Transition _transition = default;
		private StopMode _stopMode = default;
		private float _overrideFade = AudioPlayer.UseEntitySetting;

		public bool IsPlayingVirtually => IsActive && Player.MixerDecibelVolume <= AudioConstant.MinDecibelVolume;

		public MusicPlayer()
		{
		}

		public MusicPlayer(AudioPlayer audioPlayer) : base(audioPlayer)
		{
		}

		protected override void Dispose(AudioPlayer player)
		{
			_transition = default;
			_stopMode = default;
			_overrideFade = AudioPlayer.UseEntitySetting;
		}

		IMusicPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade)
		{
			_transition = transition;
			_stopMode = stopMode;
			_overrideFade = overrideFade;
			return this;
		}

		public PlaybackPreference Transition(PlaybackPreference pref)
		{
			if(CurrentPlayer != null)
			{
				pref.SetFadeTime(_transition, _overrideFade);
				switch (_transition)
				{
					case Ami.BroAudio.Transition.Immediate:
					case Ami.BroAudio.Transition.OnlyFadeIn:
					case Ami.BroAudio.Transition.CrossFade:
						StopCurrentMusic();
						break;
					case Ami.BroAudio.Transition.Default:
					case Ami.BroAudio.Transition.OnlyFadeOut:

						var waiter = pref.CreateWaiter();
						StopCurrentMusic(waiter.Finish);
						break;
				}
			}
			
			CurrentPlayer = Player;
			return pref;
		}

		private void StopCurrentMusic(Action onFinished = null)
		{
			bool noFadeOut = _transition == Ami.BroAudio.Transition.Immediate || _transition == Ami.BroAudio.Transition.OnlyFadeIn;
			float fadeOut =  noFadeOut? 0f : _overrideFade;
			CurrentPlayer.Stop(fadeOut, _stopMode, onFinished);
		}
	}
}
