using System;
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public delegate void GetSpectrumData(float[] samples, int channel, FFTWindow fftWindow);

    public class SpectrumDisplayer : MonoBehaviour
    {
        [Serializable]
        public struct Band
        {
            public Transform Transform;
            public float Frequency;
            [Range(1f, 3f)] public float Weighted;
        }

        public const int SpectrumSampleCount = 1 << 9;

        [SerializeField] float _maxHeight = 10f;
        [SerializeField] float _scale = 100f;
        [SerializeField] float _updateRate = 0.1f;
        [SerializeField] float _falldownSpeed = 0.3f;
        [SerializeField] Band[] _bands = null;
        [SerializeField] CutScenePlayer _bgmPlayer = null;

        private readonly float[] _spectrum = new float[SpectrumSampleCount];

        private float[] _buffer = null;
        private float[] _target = null;
        private float _time = 0f;
        private int _step = 0;
        private float _harmonic = 0f;

        private void Start()
        {
            _buffer = new float[_bands.Length];
            _target = _buffer.Clone() as float[];
            _bgmPlayer.OnGetSpectrumDataEventHandler += UpdateSpectrum;

            float freqRange = AudioSettings.outputSampleRate / 2f;
            _harmonic = freqRange / SpectrumSampleCount;
        }

        private void OnDestroy()
        {
            _bgmPlayer.OnGetSpectrumDataEventHandler -= UpdateSpectrum;
        }

        public void UpdateSpectrum(GetSpectrumData onGetSpectrum)
        {
            if(_time >= _updateRate)
            {
                _time = 0f;
                _step = 0;
                Array.Copy(_buffer, _target, _target.Length);
                StartCoroutine(ScaleTransform());
            }

            onGetSpectrum?.Invoke(_spectrum, 0, FFTWindow.Hanning); // Left channel

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

            _step++;
            _time += Time.deltaTime;
        }

        private int GetFrequencyBandIndex(float freq)
        {
            for(int i = 0; i < _bands.Length;i++)
            {
                if(freq < _bands[i].Frequency)
                {
                    return i;
                }
            }
            return _bands.Length - 1;
        }

        private IEnumerator ScaleTransform()
        {
            float[] previousScales = new float[_bands.Length];
            for(int i = 0; i < _bands.Length;i++)
            {
                previousScales[i] = _bands[i].Transform.localScale.y;
            }

            while (_time < _updateRate)
            {
                for(int i = 0; i < _bands.Length;i++)
                {
                    Transform bar = _bands[i].Transform;
                    float targetScale = Mathf.Min(_target[i] * _scale, _maxHeight) * _bands[i].Weighted;
                    bool isFalling = targetScale < previousScales[i];
                    float y;
                    if (isFalling)
                    {
                        y = Mathf.Max(bar.localScale.y - (_falldownSpeed * Time.deltaTime), targetScale);
                    }
                    else
                    {
                        y = Mathf.Lerp(previousScales[i], targetScale, _time / _updateRate);
                    }
                    
                    bar.localScale = new Vector3(bar.localScale.x, y, bar.localScale.z);
                }
                yield return null;
            }
        }
    } 
}