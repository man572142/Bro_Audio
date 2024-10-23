using System;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource)), AddComponentMenu("")]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        public const float UseEntitySetting = -1f;
        public const float Immediate = 0f;
        private const int DecoratorsArraySize = 2;

        public event Action<AudioPlayer> OnRecycle;

        [SerializeField] private AudioSource AudioSource = null;
        private AudioMixer _audioMixer;
        private Func<AudioTrackType, AudioMixerGroup> _getAudioTrack;

        private IBroAudioClip _clip;
        private AudioPlayerDecorator[] _decorators = null;
        private string _sendParaName = string.Empty;
		private string _currTrackName = string.Empty;
        //private string _pitchParaName = string.Empty;

        private AudioSourceProxy _proxy = null;
        private AudioFilterReader _audioFilterReader = null;

		public SoundID ID { get; private set; } = -1;

        public bool IsActive => ID > 0;
        public bool IsPlaying => AudioSource.isPlaying;
        public bool IsStopping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public EffectType CurrentActiveEffects { get; private set; } = EffectType.None;
        public bool IsUsingEffect => CurrentActiveEffects != EffectType.None;
		public bool IsDominator => TryGetDecorator<DominatorPlayer>(out _);
		public bool IsBGM => TryGetDecorator<MusicPlayer>(out _);
        IAudioSourceProxy IAudioPlayer.AudioSource
        {
            get
            {
                if(!IsActive)
                {
                    Debug.LogError(Utility.LogTitle +
                        "The audio player is not playing! Please consider accessing the AudioSource via OnStart() or OnUpdate() methods.");
                    return Empty.AudioSource;
                }
                return _proxy ??= new AudioSourceProxy(AudioSource);
            }
        }

		public string VolumeParaName 
        {
            get
			{
                if (IsUsingEffect)
				{
                    return _sendParaName;
				}
                else if(AudioTrack)
				{
                    return _currTrackName;
                }
                return string.Empty;
			}
        }

        public AudioTrackType TrackType { get; private set; } = AudioTrackType.Generic;
        public AudioMixerGroup AudioTrack 
        {
            get => AudioSource.outputAudioMixerGroup;
            private set
			{
                AudioSource.outputAudioMixerGroup = value;
                _currTrackName = value == null? string.Empty : value.name;
                _sendParaName = value == null ? string.Empty : _currTrackName + EffectParaNameSuffix;
				//_pitchParaName = value == null ? string.Empty : _currTrackName + PitchParaNameSuffix;
			}
		}

        protected virtual void Awake()
        {
            AudioSource ??= GetComponent<AudioSource>();
            InitVolumeModule();
        }

        private void Update()
        {
            if(!IsActive)
            {
                return;
            }

            if(_pref.HasFollowTarget(out var target))
            {
                transform.position = target.position;
            }
        }

        public void SetMixerData(AudioMixer mixer, Func<AudioTrackType, AudioMixerGroup> getAudioTrack)
		{
            _audioMixer = mixer;
            _getAudioTrack = getAudioTrack;
		}

		private void SetSpatial(PlaybackPreference pref)
		{
			SpatialSetting setting = pref.Entity.SpatialSetting;
			SetSpatialBlend();

			if (setting == null)
			{
				return;
			}

			AudioSource.panStereo = setting.StereoPan;
			AudioSource.dopplerLevel = setting.DopplerLevel;
			AudioSource.minDistance = setting.MinDistance;
			AudioSource.maxDistance = setting.MaxDistance;

			AudioSource.SetCustomCurveOrResetDefault(setting.ReverbZoneMix, AudioSourceCurveType.ReverbZoneMix);
			AudioSource.SetCustomCurveOrResetDefault(setting.Spread, AudioSourceCurveType.Spread);

			AudioSource.rolloffMode = setting.RolloffMode;
			if (setting.RolloffMode == AudioRolloffMode.Custom)
			{
				AudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, setting.CustomRolloff);
			}

			void SetSpatialBlend()
			{
				if (pref.HasFollowTarget(out _))
				{
					SetTo3D();
				}
				else if (pref.HasPosition(out var position))
				{
					transform.position = position;
					SetTo3D();
				}
				else if (setting != null && !setting.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D) && pref.Entity is IEntityIdentity entity)
				{
					Debug.LogWarning(Utility.LogTitle + $"You've set a non-2D SpatialBlend for :{entity.Name}, but didn't specify a position or a follow target when playing it");
				}
			}

			void SetTo3D()
			{
				if (setting != null && !setting.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D))
				{
					// Don't use SetCustomCurveOrResetDefault, it will set to 2D if isDefaultCurve.
					AudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, setting.SpatialBlend);
				}
				else
				{
					// force to 3D if it's played with a position or a follow target, even if it has no custom curve. 
					AudioSource.spatialBlend = AudioConstant.SpatialBlend_3D;
				}
			}
		}

		private void ResetSpatial()
        {
            AudioSource.spatialBlend = AudioConstant.SpatialBlend_2D;
            transform.position = Vector3.zero;

            AudioSource.panStereo = AudioConstant.DefaultPanStereo;
            AudioSource.dopplerLevel = AudioConstant.DefaultDoppler;
            AudioSource.minDistance = AudioConstant.AttenuationMinDistance;
            AudioSource.maxDistance = AudioConstant.AttenuationMaxDistance;
            AudioSource.reverbZoneMix = AudioConstant.DefaultReverZoneMix;
            AudioSource.spread = AudioConstant.DefaultSpread;
            AudioSource.rolloffMode = AudioConstant.DefaultRolloffMode;
        }

        IAudioPlayer IAudioPlayer.SetVelocity(int velocity)
        {
            _pref.SetVelocity(velocity);
            return this;
        }

        public void SetEffect(EffectType effect,SetEffectMode mode)
		{
            if((effect == EffectType.None && mode != SetEffectMode.Override) || ID <= 0)
            {
                return;
            }

			bool oldUsingEffectState = IsUsingEffect;
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
			bool newUsingEffectState = IsUsingEffect;
			if (oldUsingEffectState != newUsingEffectState)
			{
                string from = IsUsingEffect ? _currTrackName : _sendParaName;
                string to = IsUsingEffect ? _sendParaName : _currTrackName;
                _audioMixer.ChangeChannel(from, to, MixerDecibelVolume);
			}
		}

		private void ResetEffect()
		{
            if(IsUsingEffect)
            {
				_audioMixer.SafeSetFloat(_sendParaName, AudioConstant.MinDecibelVolume);
			}
            CurrentActiveEffects = EffectType.None;
		}

		IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return GetOrCreateDecorator(() => new MusicPlayer(this));
        }

#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator()
        {
            return GetOrCreateDecorator(() => new DominatorPlayer(this)); ;
        }
#endif

        private T GetOrCreateDecorator<T>(Func<T> onCreateDecorator) where T : AudioPlayerDecorator
        {
            if (_decorators != null && TryGetDecorator(out T decoratePalyer))
            {
                return decoratePalyer;
            }

            decoratePalyer = null;
            _decorators ??= new AudioPlayerDecorator[DecoratorsArraySize];
            for(int i = 0; i < _decorators.Length;i++)
            {
                if (_decorators[i] == null)
                {
                    decoratePalyer = onCreateDecorator.Invoke();
                    _decorators[i] = decoratePalyer;
                    break;
                }
            }

            if(decoratePalyer == null)
            {
                Debug.LogError(Utility.LogTitle + "Audio Player decorators array size is too small");
            }
            return decoratePalyer;
        }

        private bool TryGetDecorator<T>(out T result) where T : AudioPlayerDecorator
        {
            result = null;
            if(_decorators != null)
            {
                foreach (var deco in _decorators)
                {
                    if (deco is T target)
                    {
                        result = target;
                        return true;
                    }
                }
            }
            return false;
        }

        public void GetOutputData(float[] samples, int channels) => AudioSource.GetOutputData(samples, channels);
        public void GetSpectrumData(float[] samples, int channels, FFTWindow window) => AudioSource.GetSpectrumData(samples, channels, window);
        IAudioPlayer IAudioPlayer.OnAudioFilterRead(Action<float[], int> onAudioFilterRead)
        {
            if (!_audioFilterReader)
            {
                _audioFilterReader = gameObject.AddComponent<AudioFilterReader>();
            }
            _audioFilterReader.OnTriggerAudioFilterRead = onAudioFilterRead;
            return this;
        }

        internal void TransferEvents(out Delegate[] onUpdateDelegates, out Delegate[] onEndDelegates) 
        {
            onUpdateDelegates = null;
            onEndDelegates = null;
            if (_onUpdate != null)
            {
                onUpdateDelegates = _onUpdate.GetInvocationList();
                _onUpdate = null;
            }

            if(_onEnd != null)
            {
                onEndDelegates = _onEnd.GetInvocationList();
                _onEnd = null;
            }
        }
    }
}
