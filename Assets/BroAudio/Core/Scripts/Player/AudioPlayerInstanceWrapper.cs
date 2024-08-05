using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using System;

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

        [Obsolete("Use " + nameof(IAudioPlayer.OnEnd) + " instead")]
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

        protected override void LogInstanceIsNull()
        {
            if (SoundManager.Instance.Setting.LogAccessRecycledPlayerWarning)
            {
                Debug.LogWarning(Utility.LogTitle + "Invalid operation. The audio player you're accessing has finished playing and has been recycled.");
            }
        }


        public static implicit operator AudioPlayer(AudioPlayerInstanceWrapper wrapper) => wrapper.IsAvailable() ? wrapper.Instance : null;


        #region Interface
        public SoundID ID => Instance ? Instance.ID : SoundID.Invalid;
        public bool IsActive => IsAvailable() ? Instance.IsActive : false;
        public bool IsPlaying => IsAvailable() ? Instance.IsPlaying : false;
        IMusicPlayer IMusicDecoratable.AsBGM() => Instance ? Instance.AsBGM() : null;
#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator() => Instance.Safe()?.AsDominator();
#endif
        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => Instance.Safe()?.SetVolume(vol, fadeTime);
        IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime) => Instance.Safe()?.SetPitch(pitch, fadeTime);
        IAudioPlayer IAudioPlayer.SetVelocity(int velocity) => Instance.Safe()?.SetVelocity(velocity);

        void IAudioStoppable.Stop() => Instance.Safe()?.Stop();
        void IAudioStoppable.Stop(Action onFinished) => Instance.Safe()?.Stop(onFinished);
        void IAudioStoppable.Stop(float fadeOut) => Instance.Safe()?.Stop(fadeOut);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) => Instance.Safe()?.Stop(fadeOut, onFinished);
        void IAudioStoppable.Pause() => Instance.Safe()?.Pause();
        void IAudioStoppable.Pause(float fadeOut) => Instance.Safe()?.Pause(fadeOut);

        IAudioPlayer IAudioPlayer.OnStart(Action<IAudioPlayerContent> onStart) => Instance.Safe()?.OnStart(onStart);

        IAudioPlayer IAudioPlayer.OnUpdate(Action<IAudioPlayerContent> onUpdate) => Instance.Safe()?.OnUpdate(onUpdate);

        IAudioPlayer IAudioPlayer.OnEnd(Action onEnd) => Instance.Safe()?.OnEnd(onEnd);

        IAudioPlayer IAudioPlayer.SetOnAudioFilterRead(Action<float[], int> onAudioFilterRead) => Instance.Safe()?.SetOnAudioFilterRead(onAudioFilterRead);

        void IAudioPlayerContent.GetOutputData(float[] samples, int channels)
        {
            throw new NotImplementedException();
        }

        void IAudioPlayerContent.GetSpectrumData(float[] samples, int channels, FFTWindow window)
        {
            throw new NotImplementedException();
        }

        bool IAudioPlayerContent.GetSpatializerFloat(int index, out float value)
        {
            throw new NotImplementedException();
        }

        bool IAudioPlayerContent.GetAmbisonicDecoderFloat(int index, out float value)
        {
            throw new NotImplementedException();
        }

        AudioSource IAudioPlayerContent.GetAudioSource()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    internal static class AudioPlayerNullChecker
    {
        internal static AudioPlayer Safe(this AudioPlayer player)
        {
            if(player)
            {
                return player;
            }
            return null;
        }
    }
}