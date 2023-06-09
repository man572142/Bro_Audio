using System;
using MiProduction.BroAudio.Data;
using static MiProduction.BroAudio.Runtime.AudioPlayer;

namespace MiProduction.BroAudio.Runtime
{
	public abstract class AudioPlayerDecorator : IAudioPlayer
	{
		protected AudioPlayer Player;

		public AudioPlayerDecorator() { }

		public AudioPlayerDecorator(AudioPlayer player)
		{
			Player = player;
		}
		
		public void Init(AudioPlayer player)
		{
			Player = player;
		}

		public event Action<AudioPlayer> OnRecycle
		{
			add => Player.OnRecycle += value;
			remove => Player.OnRecycle -= value;
		}

		public int ID => Player.ID;
		public bool IsPlaying => Player.IsPlaying;

		public IAudioPlayer DuckOthers(float othersVol, float fadeTime = 0.5f) => Player.DuckOthers(othersVol, fadeTime);
		public IAudioPlayer HighPassOthers(float freq, float fadeTime = 0.5f) => Player.HighPassOthers(freq, fadeTime);
		public IAudioPlayer LowPassOthers(float freq, float fadeTime = 0.5f) => Player.LowPassOthers(freq, fadeTime);
		public IAudioPlayer SetVolume(float vol, float fadeTime = 0.5f) => Player.SetVolume(vol, fadeTime);

		public virtual void Play(int id,BroAudioClip clip, PlaybackPreference pref)
		{
			Player.Play(id,clip,pref);
		}

		public virtual void Stop(float fadeOut,Action onFinishStopping)
		{
			Player.Stop(fadeOut,onFinishStopping);
		}
		#region Stop Overloads
		public virtual void Stop() => Stop(UseClipFadeSetting);
		public virtual void Stop(float fadeOut) => Stop(fadeOut, null);
		public virtual void Stop(Action onFinishStopping) => Stop(UseClipFadeSetting,onFinishStopping);
		#endregion

	}
}
