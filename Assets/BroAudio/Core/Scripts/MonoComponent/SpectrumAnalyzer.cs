using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
    [HelpURL("https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/spectrum-analyzer")]
    [AddComponentMenu("BroAudio/" + nameof(SpectrumAnalyzer))]
    public class SpectrumAnalyzer : MonoBehaviour
    {
        public enum Channel { Left = 0, Right = 1 };

        [Serializable]
        public struct Band
        {
            public float Frequency;

            [SerializeField, Min(1f)]
            private float _weighted;

            private float _lastAmplitube;
            private float _targetAmplitube;
            public float Amplitube { get; private set; }

            internal void Record(float targetAmp, int index)
            {
                _lastAmplitube = Amplitube;
                _targetAmplitube = targetAmp * _weighted;
            }

            internal void Update(float falldown, float progress)
            {
                bool isFalling = falldown > 0f && _targetAmplitube < _lastAmplitube;
                if (isFalling)
                {
                    Amplitube = Mathf.Max(Amplitube - (falldown * Time.deltaTime), _targetAmplitube);
                }
                else
                {
                    Amplitube = Mathf.Lerp(_lastAmplitube, _targetAmplitube, progress);
                }
            }

            public static class NameOf
            {
                public const string Weighted = nameof(_weighted);
            }
        }

        public event Action<IReadOnlyList<Band>> OnUpdate;

        [SerializeField] SoundSource _soundSource = null;
        [Space]
        [SerializeField, Range(6, 13)] int _resolutionScale = 10;
        [SerializeField] float _updateRate = 0.1f;
        [SerializeField] float _scale = 100f;
        [SerializeField] float _falldownSpeed = 0f;
        [SerializeField] Channel _channel = default;
        [SerializeField] FFTWindow _windowType = default;
        [SerializeField] Band[] _bands = null;

        private float[] _spectrum = null;
        private float[] _buffer = null;
        private float _time = 0f;
        private int _step = 0;
        private float _harmonic = 0f;
        private IAudioPlayer _player;
        private bool _isUsingSoundSource = false;

        private int SpectrumSampleCount => 1 << _resolutionScale;
        public int BandCount => _bands.Length;
        public IReadOnlyList<Band> Bands => _bands;

        public void SetSource(IAudioPlayer audioPlayer)
        {
            _player = audioPlayer;
        }

        private void Start()
        {
            _spectrum = new float[SpectrumSampleCount];
            _buffer = new float[_bands.Length];

            float freqRange = AudioSettings.outputSampleRate / 2f;
            _harmonic = freqRange / SpectrumSampleCount;

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
            if (_time >= _updateRate)
            {
                for (int i = 0; i < _bands.Length; i++)
                {
                    _bands[i].Record(_buffer[i] * _scale, i);
                }
                _time = 0f;
                _step = 0;
            }

            for (int i = 0; i < _spectrum.Length - 1; i++)
            {
                float freq = i * _harmonic;
                int index = GetFrequencyBandIndex(freq);
                if (_step == 0)
                {
                    _buffer[index] = _spectrum[i];
                }
                else
                {
                    _buffer[index] = ((_buffer[index] * _step) + _spectrum[i]) / (_step + 1);
                }
            }

            for (int i = 0; i < _bands.Length;i++)
            {
                _bands[i].Update(_falldownSpeed, _time / _updateRate);
            }

            _step++;
            _time += Time.deltaTime;

            OnUpdate?.Invoke(_bands);
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
            public const string UpdateRate = nameof(_updateRate);
            public const string Scale = nameof(_scale);
            public const string FalldownSpeed = nameof(_falldownSpeed);
            public const string Channel = nameof(_channel);
            public const string WindowType = nameof(_windowType);
            public const string Bands = nameof(_bands);
        }
    }
}