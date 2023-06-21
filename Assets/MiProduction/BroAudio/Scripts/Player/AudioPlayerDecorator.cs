using System;

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

		public event Action<AudioPlayer> OnPlayerRecycle
		{
			add => Player.OnRecycle += value;
			remove => Player.OnRecycle -= value;
		}
		
		public virtual void Init(AudioPlayer player)
		{
			Player = player;
			OnPlayerRecycle += Dispose;
		}

		protected virtual void Dispose(AudioPlayer player)
		{
			OnPlayerRecycle -= Dispose;
		}

		public int ID => Player.ID;
		public bool IsPlaying => Player.IsPlaying;

		IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => Player.SetVolume(vol, fadeTime);
		IMusicPlayer IMusicDecoratable.AsMusic() => Player.AsMusic();
		IPlayerEffect IEffectDecoratable.WithEffect() => Player.WithEffect();
		IPlaybackControllable IPlaybackControlGettable.GetPlaybackControl() => Player;

		//public virtual void Play(int id,BroAudioClip clip, PlaybackPreference pref)
		//{
		//	Player.Play(id,clip,pref);
		//}

		//public virtual void Stop(float fadeOut,Action onFinishStopping)
		//{
		//	Player.Stop(fadeOut,onFinishStopping);
		//}
		//#region Stop Overloads
		//public virtual void Stop() => Stop(UseClipFadeSetting);
		//public virtual void Stop(float fadeOut) => Stop(fadeOut, null);
		//public virtual void Stop(Action onFinishStopping) => Stop(UseClipFadeSetting,onFinishStopping);
		//#endregion


	}
}
