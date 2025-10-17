using System.Collections.Generic;
using UnityEngine;
using static Ami.BroAudio.SpectrumAnalyzer;

namespace Ami.BroAudio.Demo
{
    [AddComponentMenu("")]
    public class SpectrumDisplayer : MonoBehaviour
    {
        [SerializeField] SpectrumAnalyzer _analyzer = null;
        [SerializeField] float _maxHeight = 10f;
        [SerializeField] Transform[] _barTransforms = null;
        [SerializeField] float _scale = 50f;

        private void Start()
        {
            BroAudio.OnBGMChanged += OnBGMChanged;
            if(_analyzer)
            {
                _analyzer.OnUpdate += OnSpectrumUpdate;
            }
        }

        private void OnDestroy()
        {
            BroAudio.OnBGMChanged -= OnBGMChanged;
            if (_analyzer)
            {
                _analyzer.OnUpdate -= OnSpectrumUpdate;
            }
        }

        private void OnBGMChanged(IAudioPlayer player)
        {
            _analyzer.SetSource(player);
        }

        private void OnSpectrumUpdate(IReadOnlyList<Band> bands)
        {
            for(int i = 0; i < bands.Count;i++)
            {
                if(i >= _barTransforms.Length)
                {
                    Debug.LogWarning("The bar count and spectrum band count doesn't match");
                    break;
                }

                Vector3 localScale = _barTransforms[i].localScale;
                localScale.y = Mathf.Min(bands[i].Amplitube * _scale, _maxHeight);
                _barTransforms[i].localScale = localScale;
            }
        }
    } 
}