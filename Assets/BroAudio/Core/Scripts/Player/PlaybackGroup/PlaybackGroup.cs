using System;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;

namespace Ami.BroAudio
{
    /// <summary>
    /// A ruleset that determines how sounds that assigned to this group can be played.
    /// </summary>
    /// <remarks>
    /// Please override the <see cref="InitializeRules"/> method to specify the behavior of the rules
    /// </remarks>
    public abstract class PlaybackGroup : ScriptableObject, IPlayableValidator
    {
        public delegate bool PlayableDelegate(SoundID id);

        /// <summary>
        /// Stores the value required for rule execution.
        /// </summary>
        /// <remarks>
        /// Note that the behavior needs to be specified in the <see cref="PlaybackGroup.InitializeRules"/> method using <see cref="SelectPlayableRule"/>.
        /// </remarks>
        /// <typeparam name="T">The type of the rule value</typeparam>
        [Serializable]
        public class Rule<T>
        {
            public T Value;
            [SerializeField] private bool _isOverride = true;

            public Rule(T value)
            {
                Value = value;
            }

            /// <summary>
            /// Selects the rule to be executed based on the override status.
            /// </summary>
            /// <param name="playableFunc"></param>
            /// <param name="defaultPlayableFunc"></param>
            /// <returns></returns>
            public PlayableDelegate SelectPlayableRule(PlayableDelegate playableFunc, PlayableDelegate defaultPlayableFunc = null)
            {
                if (_isOverride)
                {
                    playableFunc ??= _ => true;
                    return playableFunc;
                }
                else
                {
                    defaultPlayableFunc ??= _ => true;
                    return defaultPlayableFunc;
                }
            }

            public static implicit operator T(Rule<T> property) => property == null ? default : property.Value;
            public static implicit operator Rule<T>(T value) => new Rule<T>(value);

            public static class NameOf
            {
                public const string IsOverride = nameof(_isOverride);
            }
        }

        private List<PlayableDelegate> _rules = null;

        /// <summary>
        /// Initializes the rules that determine how the sounds can be played.
        /// </summary>
        public abstract IEnumerable<PlayableDelegate> InitializeRules();

        /// <summary>
        /// Handles the player when the sound is played.
        /// </summary>
        public virtual void HandlePlayer(IAudioPlayer player)
        {
        }

        /// <summary>
        /// Checks if the sound is playable.
        /// </summary>
        public bool IsPlayable(SoundID id)
        {
            _rules ??= new List<PlayableDelegate>(InitializeRules());

            foreach (var rule in _rules)
            {
                bool isPlayable = rule.Invoke(id);
                if(!isPlayable)
                {
                    return false;
                }
            }
            return true;
        }
    }
}