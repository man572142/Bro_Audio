using System;
using Ami.BroAudio.Tools;
using UnityEngine;
using UnityEngine.UI;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using System.Collections.Generic;

namespace Ami.BroAudio
{
    [HelpURL("https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/sound-volume")]
    [AddComponentMenu("BroAudio/" + nameof(SoundVolume))]
    public class SoundVolume : MonoBehaviour
    {
        public delegate (SliderType sliderType, bool allowBoost) GetSliderSetting();

        public const int RoundingDigits = 3;

        [Serializable]
        public class Setting
        {
            [SerializeField] BroAudioType _audioType = BroAudioType.Music;
            [SerializeField] float _volume = 1f;
            [SerializeField] Slider _slider = null;

            private GetSliderSetting _onGetSliderSetting;
            private float _originalVolume = 0f;
            private Dictionary<BroAudioType, float> _systemOriginalVolumes = null;

            private SliderType SliderType => _onGetSliderSetting.Invoke().Item1;
            private bool AllowBoost => _onGetSliderSetting.Invoke().Item2;
            public bool IsInit => _onGetSliderSetting != null;

            public void Init(GetSliderSetting onGetSliderSetting)
            {
                _onGetSliderSetting = onGetSliderSetting;
            }

            public void ApplyVolumeToSystem(float fadeTime)
            {
                BroAudio.SetVolume(_audioType, _volume, fadeTime);
            }

            public void SetVolumeToSlider(bool notify)
            {
                if (_slider)
                {
                    float value = Utility.VolumeToSlider(SliderType, _volume, AllowBoost);
                    value = (float)Math.Round(value, RoundingDigits);
                    if(notify)
                    {
                        _slider.normalizedValue = value;
                    }
                    else
                    {
                        _slider.SetValueWithoutNotify(Mathf.Lerp(_slider.minValue, _slider.maxValue, value));
                    }
                }
            }

            public void RecordOrigin()
            {
                _originalVolume = _volume;
                _systemOriginalVolumes = new Dictionary<BroAudioType, float>();
                Utility.ForeachConcreteAudioType(type => 
                {
                    if (_audioType.Contains(type) && SoundManager.Instance.TryGetAudioTypePref(type, out var pref))
                    {
                        _systemOriginalVolumes[type] = pref.Volume;
                    }
                });
            }

            public void ResetToOrigin(float fadeTime)
            {
                _volume = _originalVolume;
                SetVolumeToSlider(false);
                if (_systemOriginalVolumes != null)
                {
                    foreach (var pair in _systemOriginalVolumes)
                    {
                        BroAudio.SetVolume(pair.Key, pair.Value, fadeTime);
                    }
                    _systemOriginalVolumes = null;
                }               
            }

            public void AddSliderListener()
            {
                if(_slider)
                {
                    _slider.onValueChanged.AddListener(OnValueChanged);
                }
            }

            public void RemoveSliderListener()
            {
                if (_slider)
                {
                    _slider.onValueChanged.RemoveListener(OnValueChanged);
                }
            }

            private void OnValueChanged(float sliderValue)
            {
                float volume = Utility.SliderToVolume(SliderType, sliderValue, AllowBoost);
                _volume = volume;
                BroAudio.SetVolume(_audioType, volume);
            }

            public static class NameOf
            {
                public const string AudioType = nameof(_audioType);
                public const string Volume = nameof(_volume);
                public const string Slider = nameof(_slider);
            }
        }

        [SerializeField] bool _applyOnEnable = false;
        [SerializeField] bool _onlyApplyOnce = false;
        [SerializeField] bool _resetOnDisable = false;
        [SerializeField] float _fadeTime = 0f;

        [SerializeField] SliderType _sliderType = SliderType.BroVolume;
        [SerializeField] bool _allowBoost = false;
        [SerializeField] Setting[] _settings = null;

        private bool _hasApplyOnce = false;

        public void SetFadeTime(float fadeTime)
        {
            _fadeTime = fadeTime;
        }

        private void OnEnable()
        {
            GetSliderSetting onGetSliderSetting = null;
            foreach (var setting in _settings)
            {
                if (!setting.IsInit)
                {
                    onGetSliderSetting ??= () => (_sliderType, _allowBoost);
                    setting.Init(onGetSliderSetting);
                }

                setting.AddSliderListener();

                if(_resetOnDisable)
                {
                    setting.RecordOrigin();
                }

                if (_applyOnEnable && !(_onlyApplyOnce && _hasApplyOnce))
                {
                    setting.ApplyVolumeToSystem(_fadeTime);
                    setting.SetVolumeToSlider(false);
                    _hasApplyOnce = true;
                }
            }
        }

        private void OnDisable()
        {
            foreach (var setting in _settings)
            {
                if(_resetOnDisable)
                {
                    setting.ResetToOrigin(_fadeTime);
                    // will trigger onValueChanged and set the volume to system
                }
                setting.RemoveSliderListener();
            }
        }

        public static class NameOf
        {
            public const string ApplyOnEnable = nameof(_applyOnEnable);
            public const string OnlyApplyOnce = nameof(_onlyApplyOnce);
            public const string ResetOnDisable = nameof(_resetOnDisable);
            public const string FadeTime = nameof(_fadeTime);
            public const string SliderType = nameof(_sliderType);
            public const string AllowBoost = nameof(_allowBoost);
            public const string Settings = nameof(_settings);
        }
    }
}