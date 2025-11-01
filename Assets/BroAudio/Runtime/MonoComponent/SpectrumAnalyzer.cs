using System;
using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio
{
    [HelpURL("https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/spectrum-analyzer")]
    [AddComponentMenu("BroAudio/" + nameof(SpectrumAnalyzer))]
    public class SpectrumAnalyzer : MonoBehaviour
    {
        public enum Channel { Left = 0, Right = 1 };
        public enum Metering { Peak, RMS, Average}

        [Serializable]
        public class Band
        {
            public float Frequency;

            [SerializeField, Min(1f)]
            private float _weighted;
            public float Amplitube { get; private set; } = AudioConstant.MinVolume;
            public float DecibelVolume { get; private set; } = AudioConstant.MinDecibelVolume;

            public void SetVolume(float amp, float dB)
            {
                Amplitube = amp;
                DecibelVolume = dB;
            }

            public static class NameOf
            {
                public const string Weighted = nameof(_weighted);
            }
        }

        public const float MaxVolumeChange = 20f;

        public event Action<IReadOnlyList<Band>> OnUpdate;

        [SerializeField] SoundSource _soundSource = null;
        [Space]
        [SerializeField, Range(6, 13)] int _resolutionScale = 10;
        [SerializeField] Channel _channel = default;
        [SerializeField] FFTWindow _windowType = default;
        [SerializeField] Metering _metering = Metering.Peak;
        [SerializeField] int _attack = 100; // the time it takes to raise a level of 20dB in milliseconds
        [SerializeField] int _decay = 1500; // the time it takes to reduce a level of 20dB in milliseconds
        [SerializeField] int _smooth = 0;
        [SerializeField] Band[] _bands = null;

        private float[] _spectrum = null;
        private float _harmonic = 0f;
        private IAudioPlayer _player;
        private bool _isUsingSoundSource = false;

        private int SpectrumSampleCount => 1 << _resolutionScale;
        public int BandCount => _bands.Length;
        public IReadOnlyList<Band> Bands => _bands;
        public IReadOnlyList<float> Spectrum => _spectrum;

        public void SetSource(IAudioPlayer audioPlayer)
        {
            _player = audioPlayer;
            // TODO: Audio Listener
        }

        private void Start()
        {
            _spectrum = new float[SpectrumSampleCount];

            float nyquistFreq = AudioSettings.outputSampleRate / 2f;
            _harmonic = nyquistFreq / SpectrumSampleCount;

            _isUsingSoundSource = _soundSource != null;
        }

        private void Update()
        {
            if (_player == null && _isUsingSoundSource)
            {
                _player = _soundSource.CurrentPlayer;
            }

            if (_player != null && _player.IsPlaying)
            {
                _player.GetSpectrumData(_spectrum, (int)_channel, _windowType);
                UpdateSpectrum();
            }
        }

        private void UpdateSpectrum()
        {          
            for (int i = 0; i < _bands.Length;i++)
            {
                float minFreq = i > 0 ? _bands[i - 1].Frequency : AudioConstant.MinFrequency;
                float newVol = GetLatestAmp(minFreq, _bands[i].Frequency).ToDecibel();
                float currentVol = _bands[i].DecibelVolume;
                float diff = newVol - currentVol;
                float sign = Mathf.Sign(diff);
                float changeTime = diff > 0 ? _attack : _decay;

                float changeDegree;
                if (_smooth > 0)
                {
                    float speedRatio = diff / _smooth;
                    changeDegree = Mathf.Clamp(MaxVolumeChange * speedRatio, -MaxVolumeChange, MaxVolumeChange);
                }
                else
                {
                    changeDegree = MaxVolumeChange * sign;
                }

                float deltaTime = Utility.GetDeltaTime();
                float change = changeTime > 0 ? deltaTime * 1000 * (changeDegree / changeTime) : float.MaxValue;

                if ((diff * sign) <= change)
                {
                    currentVol = newVol;
                }
                else
                {
                    currentVol += change;
                }

                _bands[i].SetVolume(currentVol.ToNormalizeVolume(), currentVol);
            }
            OnUpdate?.Invoke(_bands);

            float GetLatestAmp(float minFreq, float maxFreq)
            {
                RangeInt range = GetFrequencyRangeIndex(minFreq, maxFreq);
                return _metering switch
                {
                    Metering.Peak => GetPeak(range),
                    Metering.RMS => GetRMS(range),
                    Metering.Average => GetAverage(range),
                    _ => throw new NotImplementedException(),
                };
            }

            float GetPeak(RangeInt range)
            {
                float peak = 0f;
                for (int i = range.start; i <= range.end; i++)
                {
                    if (_spectrum[i] > peak)
                    {
                        peak = _spectrum[i];
                    }
                }
                return peak;
            }

            float GetRMS(RangeInt range)
            {
                float sum = 0f;
                for (int i = range.start; i <= range.end; i++)
                {
                    sum += Mathf.Pow(_spectrum[i], 2);
                }
                return Mathf.Sqrt(sum / range.length);
            }

            float GetAverage(RangeInt range)
            {
                float sum = 0f;
                for (int i = range.start; i <= range.end; i++)
                {
                    sum += _spectrum[i];

                }
                return sum / range.length;
            }
        }

        private RangeInt GetFrequencyRangeIndex(float minFreq, float maxFreq)
        {
            RangeInt range;
            range.start = Mathf.CeilToInt(minFreq / _harmonic);
            int end = Mathf.FloorToInt(maxFreq / _harmonic);
            range.length = end - range.start;
            return range;
        }

        public static class NameOf
        {
            public const string SoundSource = nameof(_soundSource);
            public const string ResolutionScale = nameof(_resolutionScale);
            public const string Channel = nameof(_channel);
            public const string WindowType = nameof(_windowType);
            public const string Bands = nameof(_bands);
            public const string Metering = nameof(_metering);
            public const string Attack = nameof(_attack);
            public const string Decay = nameof(_decay);
            public const string Smooth = nameof(_smooth);
        }
    }
}