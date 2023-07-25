using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Runtime
{
	public class MusicPlayer : AudioPlayerDecorator , IMusicPlayer
	{
		public static IPlaybackControllable CurrentPlayer = null;

		private Transition _transition = default;
		private StopMode _stopMode = default;
		private float _overrideFade = AudioPlayer.UseLibraryManagerSetting;

		public bool IsBaseNull => !Player;
		public bool IsPlayingVirtually => IsPlaying && Player.MixerDecibelVolume <= AudioConstant.MinDecibelVolume;

		public MusicPlayer()
		{
		}

		public MusicPlayer(AudioPlayer audioPlayer) : base(audioPlayer)
		{
		}

		public override void Init(AudioPlayer player)
		{
			base.Init(player);

			player.DecoratePlaybackPreference += DecoratePlayback;
		}

		protected override void Dispose(AudioPlayer player)
		{
			player.DecoratePlaybackPreference -= DecoratePlayback;

			_transition = default;
			_stopMode = default;
			_overrideFade = AudioPlayer.UseLibraryManagerSetting;
		}

		IMusicPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade)
		{
			_transition = transition;
			_stopMode = stopMode;
			_overrideFade = overrideFade;
			return this;
		}

		private void DecoratePlayback(PlaybackPreference pref)
		{
			if(CurrentPlayer != null)
			{
				pref.SetFadeTime(_transition, _overrideFade);
				switch (_transition)
				{
					case Transition.Immediate:
					case Transition.OnlyFadeIn:
					case Transition.CrossFade:
						StopCurrentMusic();
						break;
					case Transition.Default:
					case Transition.OnlyFadeOut:
						pref.HaveToWaitForPrevious = true;
						StopCurrentMusic(() => pref.HaveToWaitForPrevious = false);
						break;
				}
			}
			
			CurrentPlayer = Player;
		}

		private void StopCurrentMusic(Action onFinished = null)
		{
			bool noFadeOut = _transition == Transition.Immediate || _transition == Transition.OnlyFadeIn;
			float fadeOut =  noFadeOut? 0f : _overrideFade;
			CurrentPlayer.Stop(fadeOut, _stopMode, onFinished);
		}
	}
}
