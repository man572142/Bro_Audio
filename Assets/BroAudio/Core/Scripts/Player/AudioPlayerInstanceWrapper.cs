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
#pragma warning disable UNT0008
        public SoundID ID => Instance ? Instance.ID : SoundID.Invalid;
        public bool IsActive => IsAvailable() ? Instance.IsActive : false;
        public bool IsPlaying => IsAvailable() ? Instance.IsPlaying : false;
        IMusicPlayer IMusicDecoratable.AsBGM() => Instance?.AsBGM() ?? Empty.MusicPlayer;
#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator() => Instance?.AsDominator() ?? Empty.DominatorPlayer;
#endif
        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => Instance?.SetVolume(vol, fadeTime) ?? Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime) => Instance?.SetPitch(pitch, fadeTime) ?? Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetVelocity(int velocity) => Instance?.SetVelocity(velocity) ?? Empty.AudioPlayer;

        void IAudioStoppable.Stop() => Instance?.Stop();
        void IAudioStoppable.Stop(Action onFinished) => Instance?.Stop(onFinished);
        void IAudioStoppable.Stop(float fadeOut) => Instance?.Stop(fadeOut);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) => Instance?.Stop(fadeOut, onFinished);
        void IAudioStoppable.Pause() => Instance?.Pause();
        void IAudioStoppable.Pause(float fadeOut) => Instance?.Pause(fadeOut);
        void IAudioStoppable.UnPause() => Instance?.UnPause();
        void IAudioStoppable.UnPause(float fadeOut) => Instance?.UnPause(fadeOut);

        IAudioPlayer IAudioPlayer.OnStart(Action<IAudioPlayer> onStart) => Instance?.OnStart(onStart) ?? Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnUpdate(Action<IAudioPlayer> onUpdate) => Instance?.OnUpdate(onUpdate) ?? Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnEnd(Action<SoundID> onEnd) => Instance?.OnEnd(onEnd) ?? Empty.AudioPlayer;
        IAudioSourceProxy IAudioPlayer.AudioSource
        {
            get
            {
                if(Instance && Instance is IAudioPlayer player)
                {
                    return player.AudioSource;
                }
                return null;
            }
        }

        public void GetOutputData(float[] samples, int channels) => Instance?.GetOutputData(samples, channels);

        public void GetSpectrumData(float[] samples, int channels, FFTWindow window) => Instance?.GetSpectrumData(samples, channels, window);

        IAudioPlayer IAudioPlayer.OnAudioFilterRead(Action<float[], int> onAudioFilterRead)=> Instance?.OnAudioFilterRead(onAudioFilterRead) ?? Empty.AudioPlayer;
#pragma warning restore UNT0008
        #endregion

        public override void UpdateInstance(AudioPlayer newInstance)
        {
            Instance.TransferEvents(out var onUpdateDelegates, out var onEndDelegates);
            if(onUpdateDelegates != null)
            {
                foreach(var onUpdate in onUpdateDelegates)
                {
                    newInstance.OnUpdate(onUpdate as Action<IAudioPlayer>);
                }
            }

            if (onEndDelegates != null)
            {
                foreach (var onEnd in onEndDelegates)
                {
                    newInstance.OnEnd(onEnd as Action<SoundID>);
                }
            }
             
            base.UpdateInstance(newInstance);
        }
    }
}