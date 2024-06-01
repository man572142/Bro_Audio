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

        public event Action<SoundID> OnEndPlaying
		{
			add
			{
				if(IsAvailable())
				{
					Instance.OnEndPlaying += value;
				}
			}
			remove
			{
				if(IsAvailable())
				{
					Instance.OnEndPlaying -= value;
				}
			}
		}

        public SoundID ID => Instance ? Instance.ID : -1;

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

        void IAudioStoppable.Stop() => Instance?.Stop();
        void IAudioStoppable.Stop(Action onFinished) => Instance?.Stop(onFinished);
        void IAudioStoppable.Stop(float fadeOut) => Instance?.Stop(fadeOut);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) => Instance?.Stop(fadeOut, onFinished);
        void IAudioStoppable.Pause() => Instance?.Pause();
        void IAudioStoppable.Pause(float fadeOut) => Instance?.Pause(fadeOut);
    }
}