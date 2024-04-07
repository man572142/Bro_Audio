using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;
using static Ami.BroAudio.Tools.BroLog;
using System;

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
			LogError("The audio player that you are refering to has been recycled");
		}
	}
}