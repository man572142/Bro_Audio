using UnityEngine;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using System;
using System.Collections.Generic;

namespace Ami.BroAudio
{
    /// <summary>
    /// To keep tracking the instance of an AudioPlayer
    /// </summary>
    public class AudioPlayerInstanceWrapper : InstanceWrapper<AudioPlayer>, IAudioPlayer, IMusicPlayer, IPlayerEffect
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

        private List<AudioPlayerDecorator> _decorators = null;

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
        IMusicPlayer IMusicDecoratable.AsBGM() => IsAvailable() ? Execute(Instance.AsBGM()) : Empty.MusicPlayer;
#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator() => IsAvailable() ? Execute(Instance.AsDominator()) : Empty.DominatorPlayer;
#endif
        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime) => IsAvailable() ? Execute(Instance.SetVolume(vol, fadeTime)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetPitch(float pitch, float fadeTime) => IsAvailable() ? Execute(Instance.SetPitch(pitch, fadeTime)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.SetVelocity(int velocity) => IsAvailable() ? Execute(Instance.SetVelocity(velocity)) : Empty.AudioPlayer;

        void IAudioStoppable.Stop() => Instance?.Stop();
        void IAudioStoppable.Stop(Action onFinished) => Instance?.Stop(onFinished);
        void IAudioStoppable.Stop(float fadeOut) => Instance?.Stop(fadeOut);
        void IAudioStoppable.Stop(float fadeOut, Action onFinished) => Instance?.Stop(fadeOut, onFinished);
        void IAudioStoppable.Pause() => Instance?.Pause();
        void IAudioStoppable.Pause(float fadeOut) => Instance?.Pause(fadeOut);
        void IAudioStoppable.UnPause() => Instance?.UnPause();
        void IAudioStoppable.UnPause(float fadeOut) => Instance?.UnPause(fadeOut);

        IAudioPlayer IAudioPlayer.OnStart(Action<IAudioPlayer> onStart) => IsAvailable() ? Execute(Instance.OnStart(onStart)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnUpdate(Action<IAudioPlayer> onUpdate) => IsAvailable() ? Execute(Instance.OnUpdate(onUpdate)) : Empty.AudioPlayer;
        IAudioPlayer IAudioPlayer.OnEnd(Action<SoundID> onEnd) => IsAvailable() ? Execute(Instance.OnEnd(onEnd)) : Empty.AudioPlayer;
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

        SoundID IMusicPlayer.ID => throw new NotImplementedException();

        IAudioPlayer ISchedulable.SetScheduledStartTime(double dspTime) => IsAvailable() ? Execute(Instance.SetScheduledStartTime(dspTime)) : Empty.AudioPlayer;
        IAudioPlayer ISchedulable.SetScheduledEndTime(double dspTime) => IsAvailable() ? Execute(Instance.SetScheduledEndTime(dspTime)) : Empty.AudioPlayer;
        IAudioPlayer ISchedulable.SetDelay(float time) => IsAvailable() ? Execute(Instance.SetDelay(time)) : Empty.AudioPlayer;

        public void GetOutputData(float[] samples, int channels) => Instance?.GetOutputData(samples, channels);
        public void GetSpectrumData(float[] samples, int channels, FFTWindow window) => Instance?.GetSpectrumData(samples, channels, window);
        IAudioPlayer IAudioPlayer.OnAudioFilterRead(Action<float[], int> onAudioFilterRead) => IsAvailable() ? Execute(Instance.OnAudioFilterRead(onAudioFilterRead)) : Empty.AudioPlayer;

        // These decorator methods will only be called when there's corresponding instance in the _decorators list
        IAudioPlayer IMusicPlayer.SetTransition(Transition transition, StopMode stopMode, float overrideFade)
            => Execute(GetDecorator<MusicPlayer>()?.SetTransition(transition, stopMode, overrideFade));

#if !UNITY_WEBGL
        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, float fadeTime)
            => Execute(GetDecorator<DominatorPlayer>()?.QuietOthers(othersVol, fadeTime));
        IPlayerEffect IPlayerEffect.QuietOthers(float othersVol, Fading fading)
            => Execute(GetDecorator<DominatorPlayer>()?.QuietOthers(othersVol, fading));
        IPlayerEffect IPlayerEffect.LowPassOthers(float freq, float fadeTime)
            => Execute(GetDecorator<DominatorPlayer>()?.LowPassOthers(freq, fadeTime));
        IPlayerEffect IPlayerEffect.LowPassOthers(float freq, Fading fading)
            => Execute(GetDecorator<DominatorPlayer>()?.LowPassOthers(freq, fading));

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq, float fadeTime)
            => Execute(GetDecorator<DominatorPlayer>()?.HighPassOthers(freq, fadeTime));

        IPlayerEffect IPlayerEffect.HighPassOthers(float freq, Fading fading)
            => Execute(GetDecorator<DominatorPlayer>()?.HighPassOthers(freq, fading));
#endif
#pragma warning restore UNT0008
#endregion

        public override void UpdateInstance(AudioPlayer newInstance)
        {
            if(Instance.TransferOnUpdates(out var onUpdateDelegates))
            {
                foreach(var onUpdate in onUpdateDelegates)
                {
                    newInstance.OnUpdate(onUpdate as Action<IAudioPlayer>);
                }
            }

            if (Instance.TransferOnEnds(out var onEndDelegates))
            {
                foreach (var onEnd in onEndDelegates)
                {
                    newInstance.OnEnd(onEnd as Action<SoundID>);
                }
            }
            
            if(Instance.TransferDecorators(out var decorators))
            {
                _decorators ??= new List<AudioPlayerDecorator>();
                _decorators.Clear();
                _decorators.AddRange(decorators);
            }
             
            base.UpdateInstance(newInstance);
        }

        /// <summary>
        /// Executing a method and returning the instance of this wrapper instead of the method's return value
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private IAudioPlayer Execute(object method)
        {
            return this;
        }

        private IMusicPlayer Execute(IMusicPlayer musicPlayer)
        {
            CacheDecoratorIfNeeded(musicPlayer as MusicPlayer);
            return this;
        }

        private IPlayerEffect Execute(IPlayerEffect dominator)
        {
            CacheDecoratorIfNeeded(dominator as DominatorPlayer);
            return this;
        }

        private void CacheDecoratorIfNeeded<T>(T decorator) where T : AudioPlayerDecorator
        {
            _decorators ??= new List<AudioPlayerDecorator>();
            if (!_decorators.TryGetDecorator<T>(out _))
            {
                _decorators.Add(decorator);
            }
        }

        private T GetDecorator<T>() where T : AudioPlayerDecorator
        {
            if (_decorators.TryGetDecorator<T>(out var result))
            {
                return result;
            }
            return null;
        }

        public static implicit operator AudioPlayer(AudioPlayerInstanceWrapper wrapper) => wrapper.IsAvailable() ? wrapper.Instance : null;
    }
}