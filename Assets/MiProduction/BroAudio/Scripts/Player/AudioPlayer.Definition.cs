using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        protected enum VolumeControl
        {
            Clip,
            Track,
            MixerDecibel,
        }

        private List<AudioPlayerDecorator> _decorators = null;

        public const float UseLibraryManagerSetting = -1f;
        public const float Immediate = 0f;
        private const string ExclusiveModeParaName = "_Send";

        public bool IsPlaying => AudioSource.isPlaying;
        public bool IsStopping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }
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
