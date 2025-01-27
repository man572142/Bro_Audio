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

#if UNITY_EDITOR
        [CustomDrawingMethod(typeof(DefaultPlaybackGroup), nameof(DrawMaxPlayableLimitProperty))]
#endif
        [SerializeField]
        [Tooltip("The maximum number of sounds that can be played simultaneously in this group")]
        [ValueButton("Infinity", -1)]
        private MaxPlayableCountRule _maxPlayableCount = -1;

        [SerializeField]
        [ValueButton("Default", 0.04f)]
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

        private int _currentPlayingCount;

        /// <inheritdoc cref="PlaybackGroup.InitializeRules"/>
        protected override IEnumerable<PlayableDelegate> InitializeRules()
        {
            yield return _maxPlayableCount.SetPlayableRule(IsPlayableLimitNotReached, Parent.GetRule);
            yield return _combFilteringTime.SetPlayableRule(CheckCombFiltering, Parent.GetRule);
        }

        public override IRule GetRule(Type ruleType) => ruleType switch
        {
            Type t when t == typeof(MaxPlayableCountRule) => _maxPlayableCount,
            Type t when t == typeof(CombFilteringRule) => _combFilteringTime,
            _ => throw new NotImplementedException(),
        };

        /// <summary>
        /// Handles the player when the sound is played and keeps track of the number of sounds that are currently playing
        /// </summary>
        /// <param name="player"></param>
        public override void HandlePlayer(IAudioPlayer player)
        {
            base.HandlePlayer(player);

            _currentPlayingCount++;
            player.OnEnd(_ => _currentPlayingCount--);
        }

        #region Check Rule
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
                    Debug.LogWarning(Utility.LogTitle + $"One of the plays of Audio:{((SoundID)id).ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"For more information, please go to the [Comb Filtering] section in Tools/BroAudio/Preference.");
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