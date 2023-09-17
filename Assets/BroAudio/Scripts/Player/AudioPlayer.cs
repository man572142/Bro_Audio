using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        public const float UseLibraryManagerSetting = -1f;
        public const float Immediate = 0f;
        public const string SendParaName = "_Send";
        public const float SpatialBlend_2D = 0f;
        public const float SpatialBlend_3D = 1f;

        public event Action<AudioPlayer> OnRecycle;

        [SerializeField] private AudioSource AudioSource = null;
        private AudioMixer _audioMixer;

        private IBroAudioClip CurrentClip;
        private List<AudioPlayerDecorator> _decorators = null;
        private string _sendParaName = null;
        private bool _isUsingEffect = false;

        public bool IsPlaying => AudioSource.isPlaying;
        public bool IsStopping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }
        public int ID { get; private set; } = -1;
        public EffectType CurrentActiveEffects { get; private set; } = EffectType.None;
        public string VolumeParaName 
        {
            get
			{
                if(_isUsingEffect)
				{
                    return _sendParaName;
				}
                else if(AudioTrack)
				{
                    return AudioTrack.name;
                }
                return string.Empty;
			}
        }

        public AudioMixerGroup AudioTrack 
        {
            get => AudioSource.outputAudioMixerGroup;
            set
			{
                AudioSource.outputAudioMixerGroup = value;
                _sendParaName = value == null ? null : AudioSource.outputAudioMixerGroup.name + SendParaName;
            }
        }

        protected virtual void Awake()
        {
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
            }
        }

        public void SetMixer(AudioMixer mixer)
		{
            _audioMixer = mixer;
		}

        private void SetSpatial(PlaybackPreference pref)
        {
            if(pref.HasFollowTarget(out var followTarget) && transform.parent != followTarget)
			{
                transform.SetParent(followTarget,false);
                AudioSource.spatialBlend = SpatialBlend_3D;
            }
            else if (pref.HasPosition(out var position))
			{
                transform.position = position;
                AudioSource.spatialBlend = SpatialBlend_3D;
            }
        }

        public void ResetSpatial()
        {
            AudioSource.spatialBlend = SpatialBlend_2D;
            if(transform.parent != SoundManager.Instance)
			{
                transform.SetParent(SoundManager.Instance.transform);
			}
            transform.position = Vector3.zero;
        }

        public void SetEffect(EffectType effect,SetEffectMode mode)
		{
            if(effect == EffectType.None)
            {
                return;
            }

			switch (mode)
			{
				case SetEffectMode.Add:
                    CurrentActiveEffects |= effect;
                    break;
				case SetEffectMode.Remove:
                    CurrentActiveEffects &= ~effect;
                    break;
				case SetEffectMode.Override:
                    CurrentActiveEffects = effect;
                    break;
			}

            bool newUsingEffectState = CurrentActiveEffects != EffectType.None;
			if (_isUsingEffect != newUsingEffectState)
			{
				_isUsingEffect = newUsingEffectState;
				ChangeChannel();
			}
		}

		private void ChangeChannel()
		{
			float sendVol = _isUsingEffect ? MixerDecibelVolume : AudioConstant.MinDecibelVolume;
			float mainVol = _isUsingEffect ? AudioConstant.MinDecibelVolume : MixerDecibelVolume;

			_audioMixer.SetFloat(_sendParaName, sendVol);
			_audioMixer.SetFloat(AudioTrack.name, mainVol);
		}

		IPlaybackControllable IPlaybackControlGettable.GetPlaybackControl() => this;

        IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return GetDecorator<MusicPlayer>();
        }

#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator(BroAudioType dominatedType)
        {
            return GetDecorator<AudioPlayerEffect>();
        }
#endif

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

            if(_decorators == null)
                _decorators = new List<AudioPlayerDecorator>();
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
