using System;
using System.Collections.Generic;
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

        [SerializeField] private AudioSource AudioSource = null;

        private IBroAudioClip _clip;
        private List<AudioPlayerDecorator> _decorators = null;
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
        public bool IsDominator => HasDecoratorOf<DominatorPlayer>();
        public bool IsBGM => HasDecoratorOf<MusicPlayer>();
        public IBroAudioClip CurrentPlayingClip => _clip;
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

        private string VolumeParaName 
        {
            get
            {
                if (IsUsingEffect)
                {
                    return _sendParaName;
                }
                else if(!string.IsNullOrEmpty(_currTrackName))
                {
                    return _currTrackName;
                }
                return string.Empty;
            }
        }

        private AudioTrackType TrackType { get; set; } = AudioTrackType.Generic;
        private AudioMixerGroup AudioTrack 
        {
            set
            {
                AudioSource.outputAudioMixerGroup = value;
                _currTrackName = value == null? string.Empty : value.name;
                _sendParaName = value == null ? string.Empty : _currTrackName + EffectParaNameSuffix;
                //_pitchParaName = value == null ? string.Empty : _currTrackName + PitchParaNameSuffix;
            }
        }

        IAudioMixerPool MixerPool => SoundManager.Instance;

        private bool TryGetMixerAndTrack(out AudioMixer mixer, out AudioMixerGroup track)
        {
            track = AudioSource.outputAudioMixerGroup;
            mixer = SoundManager.Instance.AudioMixer;
            return mixer != null && track != null;
        }

        protected virtual void Awake()
        {
# pragma warning disable UNT0023
            AudioSource ??= GetComponent<AudioSource>(); 
# pragma warning restore UNT0023
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
                // The log is unnecessary and may cause misunderstandings, as the Play method already provides clear summaries.
                //else if (setting != null && !setting.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D) && pref.Entity is IEntityIdentity entity)
                //{
                //	Debug.LogWarning(Utility.LogTitle + $"You've set a non-2D SpatialBlend for :{entity.Name}, but didn't specify a position or a follow target when playing it");
                //}
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
            if(ID <= 0 || (effect == EffectType.None && mode != SetEffectMode.Override) 
                || !TryGetMixerAndTrack(out var mixer, out _) || !TryGetMixerDecibelVolume(out float mixerDecibelVolume))
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
                mixer.ChangeChannel(from, to, mixerDecibelVolume);
            }
        }

        private void ResetEffect()
        {
            if(IsUsingEffect && TryGetMixerAndTrack(out var mixer, out _))
            {
                mixer.SafeSetFloat(_sendParaName, AudioConstant.MinDecibelVolume);
            }
            CurrentActiveEffects = EffectType.None;
        }

        IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return Utility.GetOrCreateDecorator(ref _decorators, () => new MusicPlayer(this));
        }

#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator()
        {
            return Utility.GetOrCreateDecorator(ref _decorators, () => new DominatorPlayer(this));
        }
#endif

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

        internal bool TransferOnUpdates(out Delegate[] onUpdateDelegates)
        {
            onUpdateDelegates = null;
            if (_onUpdate != null)
            {
                onUpdateDelegates = _onUpdate.GetInvocationList();
                _onUpdate = null;
            }
            return onUpdateDelegates != null;
        }

        internal bool TransferOnEnds(out Delegate[] onEndDelegates) 
        {
            onEndDelegates = null;
            if(_onEnd != null)
            {
                onEndDelegates = _onEnd.GetInvocationList();
                _onEnd = null;
            }
            return onEndDelegates != null;
        }

        internal bool TransferDecorators(out IReadOnlyList<AudioPlayerDecorator> decorators)
        {
            decorators = _decorators;
            _decorators = null;
            return decorators != null;
        }

        internal void SetDecorators(IReadOnlyList<AudioPlayerDecorator> decorators)
        {
            _decorators = decorators as List<AudioPlayerDecorator>;
        }

        internal bool TryGetDecorator<T>(out T decorator) where T : AudioPlayerDecorator
        {
            decorator = null;
            if (_decorators != null)
            {
                return _decorators.TryGetDecorator<T>(out decorator);
            }
            return false;
        }

        private bool HasDecoratorOf<T>() where T : AudioPlayerDecorator
        {
            if(_decorators != null)
            {
                return _decorators.TryGetDecorator<T>(out _);
            }
            return false;
        }
    }
}
