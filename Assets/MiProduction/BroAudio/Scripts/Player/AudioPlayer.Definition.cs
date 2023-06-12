using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        private List<AudioPlayerDecorator> _decorators = null;

        protected enum VolumeControl
		{
            Clip,
            Track,
            MixerDecibel,
		}

        protected enum Command
        {
            None,
            Play,

            Stop,
            Pause,
            Mute,
        }

        public const float UseLibraryManagerSetting = -1f;
        public const float Immediate = 0f;
        private const string ExclusiveModeParaName = "_Send";

        public bool IsPlaying { get; protected set; }
        public bool IsStopping { get; protected set; }
        public bool IsFadingOut { get; protected set; }
        public bool IsFadingIn { get; protected set; }
        public int ID { get; protected set; }

        IPlaybackControllable IPlaybackControlGettable.GetPlaybackControl() => this;

        IMusicPlayer IMusicDecoratable.AsMusic() 
        {
            return GetDecorator<MusicPlayer>();
        }
        IPlayerExclusive IExclusiveDecoratable.AsExclusive() 
        {
            return GetDecorator<AudioPlayerExclusiveEffect>();
        }

        private T GetDecorator<T>() where T : AudioPlayerDecorator ,new()
		{
            if (_decorators != null)
            {
                foreach (var deco in _decorators)
                {
                    if (deco is T)
                    {
                        return (T)deco;
                    }
                }
            }

            _decorators ??= new List<AudioPlayerDecorator>();
            var decoratedPalyer = this.DecorateWith<T>();
            _decorators.Add(decoratedPalyer);
            decoratedPalyer.OnPlayerRecycle += RemoveDecorator;
            return decoratedPalyer;

            void RemoveDecorator(AudioPlayer player)
            {
                _decorators = null;
            }
        }

	}
}
