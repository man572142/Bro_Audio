using System;
using Ami.BroAudio.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio
{
    [ExecuteAlways]
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

            private SliderType SliderType => _onGetSliderSetting.Invoke().Item1;
            private bool AllowBoost => _onGetSliderSetting.Invoke().Item2;

            public void Init(GetSliderSetting onGetSliderSetting)
            {
                _onGetSliderSetting = onGetSliderSetting;
            }

            public void ApplyVolumeToSystem()
            {
                BroAudio.SetVolume(_audioType, _volume);
            }

            public void SetSliderValueToVolume()
            {
                if(_slider)
                {
                    float vol = Utility.SliderToVolume(SliderType, _slider.normalizedValue, AllowBoost);
                    _volume = (float)Math.Round(vol, RoundingDigits);
                }
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
                        _slider.SetValueWithoutNotify(value);
                    }
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
                BroAudio.SetVolume(_audioType, Utility.SliderToVolume(SliderType, sliderValue, AllowBoost));
            }

            public static class NameOf
            {
                public const string AudioType = nameof(_audioType);
                public const string Volume = nameof(_volume);
                public const string Slider = nameof(_slider);
            }
        }

        [SerializeField] bool _applyOnEnable = false;
        [SerializeField] bool _resetOnDisable = false;

        [SerializeField] SliderType _sliderType = SliderType.BroVolume;
        [SerializeField] bool _allowBoost = false;
        [SerializeField] Setting[] _settings = null;

        public void ReadVolumeFromSlider()
        {
            foreach (var setting in _settings)
            {
                setting.SetSliderValueToVolume();
            }
        }

        public void SetVolumeToSlider(bool notify = true)
        {
            foreach (var setting in _settings)
            {
                setting.SetVolumeToSlider(notify);
            }
        }

        private void OnEnable()
        {
            GetSliderSetting onGetSliderSetting = () => (_sliderType, _allowBoost);
            
            if(!Application.isPlaying)
            {
                foreach (var setting in _settings)
                {
                    setting.Init(onGetSliderSetting);
                }
                return;
            }

            foreach (var setting in _settings)
            {
                setting.Init(onGetSliderSetting);
                setting.AddSliderListener();

                if (_applyOnEnable)
                {
                    setting.ApplyVolumeToSystem();
                    setting.SetVolumeToSlider(false);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var setting in _settings)
            {
                setting.RemoveSliderListener();

                if(_resetOnDisable)
                {
                    // TODO: not implemented
                }
            }
        }

        public static class NameOf
        {
            public const string ApplyOnEnable = nameof(_applyOnEnable);
            public const string ResetOnDisable = nameof(_resetOnDisable);
            public const string SliderType = nameof(_sliderType);
            public const string AllowBoost = nameof(_allowBoost);
            public const string Settings = nameof(_settings);
        }
    }
}