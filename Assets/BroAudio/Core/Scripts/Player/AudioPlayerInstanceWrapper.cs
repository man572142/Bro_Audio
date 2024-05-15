using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using System;
using static UnityEngine.Debug;

namespace Ami.BroAudio
{
	/// <summary>
	/// To keep tracking the instance of an AudioPlayer
	/// </summary>
	public class AudioPlayerInstanceWrapper : InstanceWrapper<AudioPlayer> ,IAudioPlayer
	{
		public event Action<AudioPlayer> OnWrapperRecycle;

		public AudioPlayerInstanceWrapper(AudioPlayer instance) : base(instance)
		{
			Instance.OnRecycle += OnRecycle;
		}

		public void UpdateInstance(AudioPlayer newInstance)
		{
			Instance.OnRecycle -= OnRecycle;
			Instance = newInstance;
			newInstance.OnRecycle += OnRecycle;
		}

		private void OnRecycle(AudioPlayer player)
		{
			OnWrapperRecycle?.Invoke(player);

            Instance.OnRecycle -= OnRecycle;
			Instance = null;
		}

		private AudioPlayer GetInstance() => IsAvailable() ? Instance : null;

        public int ID => IsAvailable() ? Instance.ID : -1;

		public bool IsActive => IsAvailable() ? Instance.IsActive : false;

		public bool IsPlaying => IsAvailable() ? Instance.IsPlaying : false;

		IMusicPlayer IMusicDecoratable.AsBGM() => IsAvailable() ? Instance.AsBGM() : null;

#if !UNITY_WEBGL
		IPlayerEffect IEffectDecoratable.AsDominator() => IsAvailable() ? Instance.AsDominator() : null;
#endif
		IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => IsAvailable() ? Instance.SetVolume(vol, fadeTime) : null;

		protected override void LogInstanceIsNull()
		{
			LogError(Utility.LogTitle + "The audio player that you are refering to has been recycled");
		}

        public void Stop() => GetInstance()?.Stop();
        public void Stop(float fadeOut) => GetInstance()?.Stop(fadeOut);
        public void Stop(float fadeOut, Action onFinished) => GetInstance()?.Stop(fadeOut, onFinished);
        public void Pause() => GetInstance()?.Pause();
        public void Pause(float fadeOut) => GetInstance()?.Pause(fadeOut);
    }
}