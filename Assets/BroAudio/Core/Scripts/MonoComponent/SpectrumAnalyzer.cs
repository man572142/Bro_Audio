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
        public struct Band
        {
            public float Frequency;

            [SerializeField, Min(1f)]
            private float _weighted;
            public float Amplitube { get; private set; }
            public float DecibelVolume { get; private set; }

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

        public const float VolumeChange = 20f;

        public event Action<IReadOnlyList<Band>> OnUpdate;

        [SerializeField] SoundSource _soundSource = null;
        [Space]
        [SerializeField, Range(6, 13)] int _resolutionScale = 10;
        [SerializeField] float _scale = 100f;
        [SerializeField] Channel _channel = default;
        [SerializeField] FFTWindow _windowType = default;
        [SerializeField] Metering _metering = Metering.Peak;
        [SerializeField] int _attack = 100; // the time it takes to raise a level of 20dB in milliseconds
        [SerializeField] int _decay = 1500; // the time it takes to reduce a level of 20dB in milliseconds
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
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _bands.Length;i++)
            {
                float minFreq = i > 0 ? _bands[i - 1].Frequency : AudioConstant.MinFrequency;
                GetFrequencyRangeIndex(minFreq, _bands[i].Frequency, out RangeInt range);
                float amp = GetLatestAmp(range);
                float vol = amp.ToDecibel();
                float bandVol = _bands[i].DecibelVolume;
                float diff = vol - bandVol;
                float changeTime = diff > 0 ? _attack : _decay;
                float change = changeTime > 0 ? deltaTime * 1000 * (VolumeChange / changeTime) : float.MaxValue;
                float sign = Mathf.Sign(diff);


                // todo: attack and decay meaning is changed
                if((diff * sign) <= change)
                {
                    bandVol = vol;
                }
                else
                {
                    bandVol += change * sign;
                }

                _bands[i].SetVolume(bandVol.ToNormalizeVolume() * _scale, bandVol);
            }

            OnUpdate?.Invoke(_bands);

            float GetLatestAmp(RangeInt range) => _metering switch
            {
                Metering.Peak => GetPeak(range),
                Metering.RMS => GetRMS(range),
                Metering.Average => GetAverage(range),
                _ => throw new NotImplementedException(),
            };

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

        private void GetFrequencyRangeIndex(float minFreq, float maxFreq, out RangeInt range)
        {
            range.start = Mathf.CeilToInt(minFreq / _harmonic);
            int end = Mathf.FloorToInt(maxFreq / _harmonic);
            range.length = end - range.start;
        }

        private int GetFrequencyBandIndex(float freq)
        {
            for (int i = 0; i < _bands.Length; i++)
            {
                if (freq < _bands[i].Frequency)
                {
                    return i;
                }
            }
            return _bands.Length - 1;
        }

        public static class NameOf
        {
            public const string SoundSource = nameof(_soundSource);
            public const string ResolutionScale = nameof(_resolutionScale);
            public const string Scale = nameof(_scale);
            public const string Channel = nameof(_channel);
            public const string WindowType = nameof(_windowType);
            public const string Bands = nameof(_bands);
            public const string Metering = nameof(_metering);
            public const string Attack = nameof(_attack);
            public const string Decay = nameof(_decay);
        }
    }
}