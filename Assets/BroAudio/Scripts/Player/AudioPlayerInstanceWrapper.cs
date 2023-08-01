using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;
using static Ami.BroAudio.BroLog;

namespace Ami.BroAudio
{
	public class AudioPlayerInstanceWrapper : InstanceWrapper<AudioPlayer> ,IAudioPlayer
	{
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
			Instance.OnRecycle -= OnRecycle;
			Instance = null;
		}

		public int ID => IsAvailable() ? Instance.ID : -1;

		public bool IsPlaying => IsAvailable() ? Instance.IsPlaying : false;

		IMusicPlayer IMusicDecoratable.AsBGM() => IsAvailable() ? Instance.AsBGM() : null;

		IPlayerEffect IEffectDecoratable.AsInvader() => IsAvailable() ? Instance.AsInvader() : null;

		IPlaybackControllable IPlaybackControlGettable.GetPlaybackControl() => IsAvailable() ? Instance.GetPlaybackControl() : null;

		IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => IsAvailable() ? Instance.SetVolume(vol, fadeTime) : null;

		protected override void LogInstanceIsNull()
		{
			LogError("The audio player that you are refering to has been recycled");
		}
	}
}