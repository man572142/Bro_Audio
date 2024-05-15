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
		
		public virtual void Init(AudioPlayer player)
		{
			Player = player;
			player.OnRecycle += Dispose;
		}

		protected virtual void Dispose(AudioPlayer player)
		{
            player.OnRecycle -= Dispose;
			Player = null;
		}

		public int ID => Player.ID;
		public bool IsActive => Player.IsActive;

		public bool IsPlaying => Player.IsPlaying;

		IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => Player.SetVolume(vol, fadeTime);
		IMusicPlayer IMusicDecoratable.AsBGM() => Player.AsBGM();
#if !UNITY_WEBGL
		IPlayerEffect IEffectDecoratable.AsDominator() => Player.AsDominator();

        public void Stop() => Player.Stop();
        public void Stop(float fadeOut) => Player.Stop(fadeOut);
        public void Stop(float fadeOut, Action onFinished) => Player.Stop(fadeOut, onFinished);
        public void Pause() => Player.Pause();
        public void Pause(float fadeOut) => Player.Pause(fadeOut);
#endif
    }
}
