using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Demo
{
    public class PreferenceOverrider : MonoBehaviour
    {
        [SerializeField] private bool _logCombFilteringWarning = false;
        [SerializeField] private float _combFilteringPreventTime = 0.04f;
        [SerializeField] private FilterSlope _audioFilterSlope = FilterSlope.FourPole;

        void Start()
        {
            var preference = SoundManager.Instance.Setting;
            preference.LogCombFilteringWarning = _logCombFilteringWarning;
            preference.CombFilteringPreventionInSeconds = _combFilteringPreventTime;
            preference.AudioFilterSlope = _audioFilterSlope;
        }

        private void OnDestroy()
        {
            SoundManager.Instance.Setting.ResetToFactorySettings();
        }
    } 
}