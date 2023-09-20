using System;

namespace Ami.BroAudio.Runtime
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
		IMusicPlayer IMusicDecoratable.AsBGM() => Player.AsBGM();
#if !UNITY_WEBGL
		IPlayerEffect IEffectDecoratable.AsDominator(BroAudioType dominatedType) => Player.AsDominator(dominatedType);
#endif
	}
}
