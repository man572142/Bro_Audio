using System;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using UnityEditor;
using UnityEngine;

namespace Ami.BroAudio
{
    /// <inheritdoc cref="PlaybackGroup"/>
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/Playback Group", fileName = "PlaybackGroup", order = 0)]
    public partial class DefaultPlaybackGroup : PlaybackGroup
    {
        public const float DefaultCombFilteringTime = RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds;

        #region Max Playable Count Rule
#if UNITY_EDITOR
        [CustomDrawingMethod(typeof(DefaultPlaybackGroup), nameof(DrawMaxPlayableLimitProperty))]
#endif
        [SerializeField]
        [Tooltip("The maximum number of sounds that can be played simultaneously in this group")]
        [ValueButton("Infinity", -1)]
        private MaxPlayableCountRule _maxPlayableCount = -1; 
        #endregion

        #region Comb-Filtering Rule
        [SerializeField]
        [ValueButton("Default", DefaultCombFilteringTime)]
        [Tooltip("Time interval to prevent the Comb-Filtering effect")]
        private CombFilteringRule _combFilteringTime = DefaultCombFilteringTime;

        [SerializeField]
        [DerivativeProperty]
        [InspectorName("Ignore If Same Frame")]
        [Tooltip("Ignore the Comb-Filtering prevention if the sound is played within the same frame")]
        private bool _ignoreCombFilteringIfSameFrame = false;

        [SerializeField]
        [DerivativeProperty(isEnd: true)]
        [InspectorName("Log Warning When Occurs")]
        [Tooltip("Log a warning message when the Comb-Filtering occurs")]
        private bool _logCombFilteringWarning = true; 
        #endregion

        private int _currentPlayingCount;

        /// <inheritdoc cref="PlaybackGroup.InitializeRules"/>
        protected override IEnumerable<IRule> InitializeRules()
        {
            yield return Initialize(_maxPlayableCount, IsPlayableLimitNotReached);
            yield return Initialize(_combFilteringTime, CheckCombFiltering);
        }

        /// <summary>
        /// Manages the player and tracks the number of sounds currently playing.
        /// </summary>
        /// <param name="player"></param>
        public override void OnGetPlayer(IAudioPlayer player)
        {
            _currentPlayingCount++;
            player.OnEnd(_ => _currentPlayingCount--);
        }

        #region Rule Methods
        protected virtual bool IsPlayableLimitNotReached(SoundID id)
        {
            return _maxPlayableCount <= 0 || _currentPlayingCount < _maxPlayableCount;
        }

        protected virtual bool CheckCombFiltering(SoundID id)
        {
            if (!SoundManager.Instance.HasPassCombFilteringPreventionTime(id, _combFilteringTime, _ignoreCombFilteringIfSameFrame))
            {
                if (_logCombFilteringWarning)
                {
                    Debug.LogWarning(Utility.LogTitle + $"One of the plays of Audio:{((SoundID)id).ToName().ToWhiteBold()} was rejected by the [Comb Filtering Time] rule.");
                }
                return false;
            }
            return true;
        }
        #endregion

        private void OnEnable()
        {
            _currentPlayingCount = 0;
        }
    }


#if UNITY_EDITOR
    public partial class DefaultPlaybackGroup : PlaybackGroup
    {
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
    }
#endif
}