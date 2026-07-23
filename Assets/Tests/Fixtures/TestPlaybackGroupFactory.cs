using Ami.BroAudio;
using UnityEngine;

namespace Ami.BroAudio.Tests.Fixtures
{
    /// <summary>
    /// Builds a <see cref="DefaultPlaybackGroup"/> (the shipped concrete <see cref="PlaybackGroup"/>) with its
    /// Max-Playable-Count and Comb-Filtering rules set directly via reflection, for playback-group scenarios.
    /// </summary>
    internal static class TestPlaybackGroupFactory
    {
        /// <param name="maxPlayableCount">-1 means unlimited (the rule's own default).</param>
        /// <param name="combFilteringTime">0 disables the comb-filtering rule.</param>
        internal static DefaultPlaybackGroup Create(int maxPlayableCount = -1, float combFilteringTime = 0f)
        {
            var group = ScriptableObject.CreateInstance<DefaultPlaybackGroup>();
            ReflectionUtility.SetField(group, "_maxPlayableCount", new MaxPlayableCountRule(maxPlayableCount));
            ReflectionUtility.SetField(group, "_combFilteringTime", new CombFilteringRule(combFilteringTime));
            return group;
        }
    }
}
