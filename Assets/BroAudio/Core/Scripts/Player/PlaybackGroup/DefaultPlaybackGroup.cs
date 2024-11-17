using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio
{
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/Playback Group", fileName = "PlaybackGroup", order = 0)]
    public class DefaultPlaybackGroup : PlaybackGroup
    {
        public const float DefaultCombFilteringTime = RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds;

        [SerializeField]
        [Tooltip("The maximum number of sounds that can be played simultaneously in this group")]
        [Button("Infinity", -1)]
        [CustomDrawingMethod(typeof(DefaultPlaybackGroup), nameof(DrawMaxPlayableLimitProperty))]
        private Rule<int> _maxPlayableCount = -1;

        [SerializeField]
        [Button("Default", 0.04f)]
        [Tooltip("Time interval to prevent the Comb-Filtering effect")]
        private Rule<float> _combFilteringTime = DefaultCombFilteringTime;

        [SerializeField]
        [DerivativeProperty]
        [InspectorName("Ignore If Same Frame")]
        [Tooltip("Ignore the Comb-Filtering prevention if the sound is played within the same frame")]
        private bool _ignoreCombFilteringIfSameFrame = false;

        [SerializeField]
        [DerivativeProperty(isEnd: true)]
        [InspectorName("Log Warning When Occurs")]
        [Tooltip("Log a warning message when the Comb-Filtering occurs")]
        private bool _logCombFilteringWarning = false;

        private int _currentPlayingCount;

        public override IEnumerable<PlayableDelegate> InitializeRules()
        {
            yield return _maxPlayableCount.Initialize(IsPlayableLimitReached);
            yield return _combFilteringTime.Initialize(CheckCombFiltering);
        }

        public override void HandlePlayer(IAudioPlayer player)
        {
            base.HandlePlayer(player);

            _currentPlayingCount++;
            player.OnEnd(_ => _currentPlayingCount--);
        }

        #region Check Rule
        protected virtual bool IsPlayableLimitReached(SoundID id)
        {
            return _maxPlayableCount <= 0 || _currentPlayingCount < _maxPlayableCount;
        }

        protected virtual bool CheckCombFiltering(SoundID id)
        {
            if (!SoundManager.Instance.HasPassCombFilteringPreventionTime(id, _combFilteringTime, _ignoreCombFilteringIfSameFrame))
            {
#if UNITY_EDITOR
                if (SoundManager.Instance.Setting.LogCombFilteringWarning)
                {
                    Debug.LogWarning(Utility.LogTitle + $"One of the plays of Audio:{((SoundID)id).ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"For more information, please go to the [Comb Filtering] section in Tools/BroAudio/Preference.");
                }
#endif
                return false;
            }
            return true;
        } 
        #endregion

        private void OnEnable()
        {
            _currentPlayingCount = 0;
        }

#if UNITY_EDITOR
        private static object DrawMaxPlayableLimitProperty(SerializedProperty property)
        {
            float currentValue = property.intValue <= 0 ? float.PositiveInfinity : property.intValue;
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(currentValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (newValue <= 0f || float.IsInfinity(newValue) || float.IsNaN(newValue))
                {
                    property.intValue = -1;
                }
                else
                {
                    property.intValue = newValue > currentValue ? Mathf.CeilToInt(newValue) : Mathf.FloorToInt(newValue);
                }
            }
            return property.intValue <= 0;
        }

        public static class NameOf
        {
            public const string CombFilteringTime = nameof(_combFilteringTime);
        }
#endif
    }
}