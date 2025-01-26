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
            add { if (IsAvailable()) Instance.OnEndPlaying += value; }
            remove { if(IsAvailable()) Instance.OnEndPlaying -= value; }
        }

        protected override void LogInstanceIsNull()
        {
            if (SoundManager.Instance.Setting.LogAccessRecycledPlayerWarning)
            {
                Debug.LogWarning(Utility.LogTitle + "Invalid operation. The audio player you're accessing has finished playing and has been recycled.");
            }
        }

        #region Interface
#pragma warning disable UNT0008
        public SoundID ID => IsAvailable() ? Instance.ID : SoundID.Invalid;
        public bool IsActive => IsAvailable(false) && Instance.IsActive;
        public bool IsPlaying => IsAvailable(false) && Instance.IsPlaying;
        // Todo: Decorator's instance is not updated when it's in seamlessLoop
        IMusicPlayer IMusicDecoratable.AsBGM() => Instance?.AsBGM() ?? Empty.MusicPlayer;
#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator() => Instance?.AsDominator() ?? Empty.DominatorPlayer;
#endif
        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => IsAvailable(out var x) ? x.Invoke(Instance.SetVolume(vol, fadeTime)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime) => IsAvailable(out var x) ? x.Invoke(Instance.SetPitch(pitch, fadeTime)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetVelocity(int velocity) => IsAvailable(out var x) ? x.Invoke(Instance.SetVelocity(velocity)) : Empty.AudioPlayer;

        void IAudioStoppable.Stop() => Instance?.Stop();
        void IAudioStoppable.Stop(Action onFinished) => Instance?.Stop(onFinished);
        void IAudioStoppable.Stop(float fadeOut) => Instance?.Stop(fadeOut);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) => Instance?.Stop(fadeOut, onFinished);
        void IAudioStoppable.Pause() => Instance?.Pause();
        void IAudioStoppable.Pause(float fadeOut) => Instance?.Pause(fadeOut);
        void IAudioStoppable.UnPause() => Instance?.UnPause();
        void IAudioStoppable.UnPause(float fadeOut) => Instance?.UnPause(fadeOut);

        IAudioPlayer IAudioPlayer.OnStart(Action<IAudioPlayer> onStart) => IsAvailable(out var x) ? x.Invoke(Instance.OnStart(onStart)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnUpdate(Action<IAudioPlayer> onUpdate) => IsAvailable(out var x) ? x.Invoke(Instance.OnUpdate(onUpdate)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnEnd(Action<SoundID> onEnd) => IsAvailable(out var x) ? x.Invoke(Instance.OnEnd(onEnd)) : Empty.AudioPlayer;
        IAudioSourceProxy IAudioPlayer.AudioSource
        {
            get
            {
                if (Instance && Instance is IAudioPlayer player)
                {
                    return player.AudioSource;
                }
                return null;
            }
        }

        IAudioPlayer ISchedulable.SetScheduledStartTime(double dspTime) => IsAvailable(out var x) ? x.Invoke(Instance.SetScheduledStartTime(dspTime)) : Empty.AudioPlayer;
        IAudioPlayer ISchedulable.SetScheduledEndTime(double dspTime) => IsAvailable(out var x) ? x.Invoke(Instance.SetScheduledEndTime(dspTime)) : Empty.AudioPlayer;
        IAudioPlayer ISchedulable.SetDelay(float time) => IsAvailable(out var x) ? x.Invoke(Instance.SetDelay(time)) : Empty.AudioPlayer;

        public void GetOutputData(float[] samples, int channels) => Instance?.GetOutputData(samples, channels);
        public void GetSpectrumData(float[] samples, int channels, FFTWindow window) => Instance?.GetSpectrumData(samples, channels, window);
        IAudioPlayer IAudioPlayer.OnAudioFilterRead(Action<float[], int> onAudioFilterRead) => IsAvailable(out var x) ? x.Invoke(Instance.OnAudioFilterRead(onAudioFilterRead)) : Empty.AudioPlayer;
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

        private bool IsAvailable(out BlindInvoker<IAudioPlayer> adapter, bool logWarning = true)
        {
            adapter = default;
            if (IsAvailable(logWarning))
            {
                adapter = new BlindInvoker<IAudioPlayer>(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ensures that any method invocation always returns the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private struct BlindInvoker<T> where T : class
        {
            private readonly T _returnValue;

            public BlindInvoker(T returnValue)
            {
                _returnValue = returnValue;
            }

            public T Invoke(object method)
            {
                return _returnValue;
            }
        }

        public static implicit operator AudioPlayer(AudioPlayerInstanceWrapper wrapper) => wrapper.IsAvailable() ? wrapper.Instance : null;
    }
}