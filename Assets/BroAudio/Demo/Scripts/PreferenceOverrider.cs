using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Demo
{
    public class PreferenceOverrider : MonoBehaviour
    {
        [SerializeField] private bool _logCombFilteringWarning = false;

        private bool _originLogCombFilteringWarning = false;

#if UNITY_EDITOR
        void Start()
        {
            var preference = SoundManager.Instance.Setting;
            _originLogCombFilteringWarning = preference.LogCombFilteringWarning;
            preference.LogCombFilteringWarning = _logCombFilteringWarning;
        }

        private void OnDestroy()
        {
            SoundManager.Instance.Setting.LogCombFilteringWarning = _originLogCombFilteringWarning;
        }
#endif
    } 
}