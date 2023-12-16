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

        public event Action<AudioPlayer> OnRecycle;

        [SerializeField] private AudioSource AudioSource = null;
        private AudioMixer _audioMixer;

        private IBroAudioClip CurrentClip;
        private List<AudioPlayerDecorator> _decorators = null;
        private string _sendParaName = null;
        private string _pitchParaName = null;
        private bool _isUsingEffect = false;
        private string _currTrackName = null;

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
                if (_isUsingEffect)
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

        public AudioMixerGroup AudioTrack 
        {
            get => AudioSource.outputAudioMixerGroup;
            set
			{
                AudioSource.outputAudioMixerGroup = value;
                _currTrackName = value == null? null : value.name;
                _sendParaName = value == null ? null : _currTrackName + SendParaNameSuffix;
                _pitchParaName = value == null ? null : _currTrackName + PitchParaNameSuffix;
            }
        }

        public bool IsDominator => TryGetDecorator<EffectDominator>(out _);

        protected virtual void Awake()
        {
            AudioSource = AudioSource ?? GetComponent<AudioSource>();
        }

		public void SetMixer(AudioMixer mixer)
		{
            _audioMixer = mixer;
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
                    _audioMixer.SetFloat(_pitchParaName, pitch); // Don't * 100f, the value in percentage is displayed in Editor only.  
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

        public void ResetSpatial()
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
            if(effect == EffectType.None && mode != SetEffectMode.Override)
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
			_audioMixer.SetFloat(_currTrackName, mainVol);
		}

        IMusicPlayer IMusicDecoratable.AsBGM()
        {
            return GetOrCreateDecorator<MusicPlayer>();
        }

#if !UNITY_WEBGL
        IPlayerEffect IEffectDecoratable.AsDominator(BroAudioType dominatedType)
        {
            EffectDominator dominator = GetOrCreateDecorator<EffectDominator>();
            dominator.SetDominatedType(dominatedType);
            return dominator;
        }
#endif

        private T GetOrCreateDecorator<T>() where T : AudioPlayerDecorator, new()
        {
            if (_decorators != null && TryGetDecorator(out T decoratedPalyer))
            {
                return decoratedPalyer;
            }

            _decorators = _decorators ?? new List<AudioPlayerDecorator>();
            decoratedPalyer = this.DecorateWith<T>();
            _decorators.Add(decoratedPalyer);
            decoratedPalyer.OnPlayerRecycle += RemoveDecorator;
            return decoratedPalyer;

            void RemoveDecorator(AudioPlayer player)
            {
                decoratedPalyer.OnPlayerRecycle -= RemoveDecorator;
                _decorators = null;
            }
        }

        private bool TryGetDecorator<T>(out T result) where T : AudioPlayerDecorator, new()
        {
            result = null;
            if(_decorators != null)
            {
                foreach (var deco in _decorators)
                {
                    if (deco is T targt)
                    {
                        result = targt;
                        return true;
                    }
                }
            }
            return false;
        }

        private IEnumerator Recycle()
        {
            yield return null;
            MixerDecibelVolume = AudioConstant.MinDecibelVolume;
            OnRecycle?.Invoke(this);
        }
	}
}
