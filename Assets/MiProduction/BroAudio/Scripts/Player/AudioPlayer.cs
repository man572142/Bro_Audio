using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.BroAudio.Data;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        public const float UseLibraryManagerSetting = -1f;
        public const float Immediate = 0f;
        public const string SendParaName = "_Send";

        public event Action<AudioPlayer> OnRecycle;
        public event Action<PlaybackPreference> DecoratePlaybackPreference;

        [SerializeField] private AudioSource AudioSource = null;
        [SerializeField] private AudioMixer AudioMixer;

        // TODO : Don't use instance
        private BroAudioClip CurrentClip;
        private List<AudioPlayerDecorator> _decorators = null;
        private string _sendParaName = null;
        private bool _isUsingEffect = false;

        public bool IsPlaying => AudioSource.isPlaying;
        public bool IsStopping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }
        public int ID { get; private set; }
        public bool IsUsingEffect 
        { 
            get => _isUsingEffect;
            private set 
            {
                _isUsingEffect = value;
                if (_isUsingEffect && AudioTrack && string.IsNullOrEmpty(_sendParaName))
                {
                    _sendParaName = AudioTrack.name + SendParaName;
                }
            } 
        }
        public string VolumeParaName => IsUsingEffect ? _sendParaName : AudioTrack.name;
        public AudioMixerGroup AudioTrack 
        {
            get => AudioSource.outputAudioMixerGroup;
            set => AudioSource.outputAudioMixerGroup = value;
        }

        protected virtual void Awake()
        {
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
            }
            if (AudioMixer == null)
            {
                LogError($"Please assign BroAudioMixer in the {nameof(AudioPlayer)}.prefab !");
            }
        }

		public void SetEffectMode(bool isOn)
		{     
            bool hasChanged = IsUsingEffect != isOn;       
            IsUsingEffect = isOn;
            if (IsPlaying && hasChanged)
			{
                ChangeChannel();
            }
        }

        private void ChangeChannel()
		{
            float sendVol = IsUsingEffect ? MixerDecibelVolume : AudioConstant.MinDecibelVolume;
            float mainVol = IsUsingEffect ? AudioConstant.MinDecibelVolume : MixerDecibelVolume;

            AudioMixer.SetFloat(_sendParaName, sendVol);
            AudioMixer.SetFloat(AudioTrack.name, mainVol);
        }

        IPlaybackControllable IPlaybackControlGettable.GetPlaybackControl() => this;

        IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return GetDecorator<MusicPlayer>();
        }
        IPlayerEffect IEffectDecoratable.AsInvader()
        {
            return GetDecorator<AudioPlayerEffect>();
        }

        private T GetDecorator<T>() where T : AudioPlayerDecorator, new()
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
                decoratedPalyer.OnPlayerRecycle -= RemoveDecorator;
                _decorators = null;
            }
        }

        private IEnumerator Recycle()
        {
            yield return null;
            MixerDecibelVolume = AudioConstant.MinDecibelVolume;
            OnRecycle?.Invoke(this);
        }
	}
}
