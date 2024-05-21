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
	public class AudioPlayerInstanceWrapper : InstanceWrapper<AudioPlayer>, IAudioPlayer
	{
		public AudioPlayerInstanceWrapper(AudioPlayer instance) : base(instance)
		{
		}

        public int ID => Instance ? Instance.ID : -1;

		public bool IsActive => Instance ? Instance.IsActive : false;

		public bool IsPlaying => Instance ? Instance.IsPlaying : false;

		IMusicPlayer IMusicDecoratable.AsBGM() => Instance ? Instance.AsBGM() : null;

#if !UNITY_WEBGL
		IPlayerEffect IEffectDecoratable.AsDominator() => Instance ? Instance.AsDominator() : null;
#endif
		IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => Instance ? Instance.SetVolume(vol, fadeTime) : null;

		protected override void LogInstanceIsNull()
		{
			if(SoundManager.Instance.Setting.LogAccessRecycledPlayerWarning)
			{
                LogWarning(Utility.LogTitle + "Invalid operation. The audio player you're accessing has finished playing and has been recycled.");
            }
		}

        public void Stop() => Instance?.Stop();
        public void Stop(Action onFinished) => Instance?.Stop(onFinished);
        public void Stop(float fadeOut) => Instance?.Stop(fadeOut);
        public void Stop(float fadeOut, Action onFinished) => Instance?.Stop(fadeOut, onFinished);
        public void Pause() => Instance?.Pause();
        public void Pause(float fadeOut) => Instance?.Pause(fadeOut);
    }
}