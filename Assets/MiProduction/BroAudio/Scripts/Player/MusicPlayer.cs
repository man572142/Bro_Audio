using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
	public class MusicPlayer : AudioPlayerDecorator
	{
		public enum Mode
		{
			Normal,
			Resume,
			Replay,
		}

		public bool IsBaseNull => !Player;
		public bool IsPlayingVirtually => IsPlaying && Player.MixerDecibelVolume <= AudioConstant.MinDecibelVolume;

		public MusicPlayer() { }

		public MusicPlayer(AudioPlayer audioPlayer) : base(audioPlayer)
		{
		}

		public void Mute(float fadeTime = 0.5f)
		{
			Player.SetVolume(0f, fadeTime);
		}

		public void Resume(float fadeTime = 0.5f)
		{
			Player.SetVolume(1f,fadeTime);

		}

		public void Replay()
		{

		}

	}
}
