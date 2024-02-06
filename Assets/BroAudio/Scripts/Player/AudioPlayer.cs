using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource)), AddComponentMenu("")]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        public const float UseEntitySetting = -1f;
        public const float Immediate = 0f;
        private const int DecoratorsArraySize = 2;

        public event Action<AudioPlayer> OnRecycle;

        [SerializeField] private AudioSource AudioSource = null;
        private AudioMixer _audioMixer;
        private Func<AudioTrackType, AudioMixerGroup> _getAudioTrack;

        private IBroAudioClip CurrentClip;
        private AudioPlayerDecorator[] _decorators = null;
        private string _sendParaName = string.Empty;
		private string _currTrackName = string.Empty;
		//private string _pitchParaName = string.Empty;

		public int ID { get; private set; } = -1;
        public bool IsPlaying => ID > 0; // AudioSource.isPlaying can't represent the actual playback state in our system
        public bool IsStopping { get; private set; }
        public bool IsFadingOut { get; private set; }
        public bool IsFadingIn { get; private set; }
        public EffectType CurrentActiveEffects { get; private set; } = EffectType.None;
        public bool IsUsingEffect => CurrentActiveEffects != EffectType.None;
		public bool IsDominator => TryGetDecorator<DominatorPlayer>(out _);
		public bool IsBGM => TryGetDecorator<MusicPlayer>(out _);
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
            AudioSource = AudioSource ?? GetComponent<AudioSource>();
        }

		public void SetData(AudioMixer mixer, Func<AudioTrackType, AudioMixerGroup> getAudioTrack)
		{
            _audioMixer = mixer;
            _getAudioTrack = getAudioTrack;
		}

        private void SetPitch(IAudioEntity entity)
        {
            float pitch = entity.Pitch;
            if(entity.RandomFlags.Contains(RandomFlags.Pitch))
			{
                float half = entity.PitchRandomRange * 0.5f;
                pitch += UnityEngine.Random.Range(-half, half);
			}

			switch (SoundManager.PitchSetting)
			{
				case PitchShiftingSetting.AudioMixer:
					//_audioMixer.SafeSetFloat(_pitchParaName, pitch); // Don't * 100f, the value in percentage is displayed in Editor only.  
					break;
				case PitchShiftingSetting.AudioSource:
                    AudioSource.pitch = pitch;
                    break;
			}
		}

		private void SetSpatial(PlaybackPreference pref)
        {
            SpatialSettings settings = pref.Entity.SpatialSettings;
            AudioSource.panStereo = settings.StereoPan;
            bool isDefault = settings == default;
            
            AudioSource.dopplerLevel = isDefault? AudioConstant.DefaultDoppler : settings.DopplerLevel;
            AudioSource.minDistance = isDefault ? AudioConstant.AttenuationMinDistance : settings.MinDistance;
            AudioSource.maxDistance = isDefault ? AudioConstant.AttenuationMaxDistance : settings.MaxDistance;

            AudioSource.SetCustomCurveOrResetDefault(settings.ReverbZoneMix, AudioSourceCurveType.ReverbZoneMix);
            AudioSource.SetCustomCurveOrResetDefault(settings.Spread, AudioSourceCurveType.Spread);
            
            AudioSource.rolloffMode = settings.RolloffMode;
            if (settings.RolloffMode == AudioRolloffMode.Custom)
            {
                AudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, settings.CustomRolloff);
            }

            if (pref.FollowTarget != null && transform.parent != pref.FollowTarget)
            {
                transform.SetParent(pref.FollowTarget, false);
                SetSpatialBlendTo3D();
            }
            else if (pref.HasPosition(out var position))
			{
                transform.position = position;
                SetSpatialBlendTo3D();
            }
            else if(!settings.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D) && pref.Entity is IEntityIdentity entity)
            {
                Debug.LogWarning($"You've set a non-2D SpatialBlend for :{entity.Name}, but didn't specify a position or a follow target when playing it");
            }

            void SetSpatialBlendTo3D()
            {
                if (!settings.SpatialBlend.IsDefaultCurve(AudioConstant.SpatialBlend_2D))
                {
                    AudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, settings.SpatialBlend);
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
            if (transform.parent != SoundManager.Instance)
			{
                transform.SetParent(SoundManager.Instance.transform);
			}
            transform.position = Vector3.zero;
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
            CurrentActiveEffects = EffectType.None;
			_audioMixer.SafeSetFloat(_sendParaName, AudioConstant.MinDecibelVolume);
		}

		IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return GetOrCreateDecorator<MusicPlayer>();
        }

#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator(BroAudioType dominatedType)
        {
            DominatorPlayer dominator = GetOrCreateDecorator<DominatorPlayer>();
            dominator.SetDominatedType(dominatedType);
            return dominator;
        }
#endif

        private T GetOrCreateDecorator<T>() where T : AudioPlayerDecorator, new()
        {
            if (_decorators != null && TryGetDecorator(out T decoratePalyer))
            {
                return decoratePalyer;
            }

            decoratePalyer = null;
            _decorators = _decorators ?? new AudioPlayerDecorator[DecoratorsArraySize];
            for(int i = 0; i < _decorators.Length;i++)
            {
                if (_decorators[i] == null)
                {
                    decoratePalyer = this.DecorateWith<T>();
                    _decorators[i] = decoratePalyer;
                    break;
                }
            }

            if(decoratePalyer == null)
            {
                Debug.LogError("Audio Player decorators array size is too small");
            }
            return decoratePalyer;
        }

        private bool TryGetDecorator<T>(out T result) where T : AudioPlayerDecorator, new()
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

        private void Recycle()
        {
            TrackType = AudioTrackType.Generic;
            MixerDecibelVolume = AudioConstant.MinDecibelVolume;
            OnRecycle?.Invoke(this);
            _decorators = null;
        }
	}
}
