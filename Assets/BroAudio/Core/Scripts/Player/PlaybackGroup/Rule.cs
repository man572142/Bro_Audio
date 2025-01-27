using System;
using UnityEngine;
using static Ami.BroAudio.PlaybackGroup;

namespace Ami.BroAudio
{
    /// <summary>
    /// Stores the value required for rule execution.
    /// </summary>
    /// <remarks>
    /// Note that the behavior needs to be specified in the <see cref="PlaybackGroup.InitializeRules"/> method using <see cref="SetPlayableRule"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the rule value</typeparam>
    [Serializable]
    public abstract class Rule<T>
    {
        public T Value;
        private PlayableDelegate _playableDelegate;
        private Func<Type, PlayableDelegate> _onGetParentRule;
        [SerializeField] private bool _isOverride = true;

        public PlayableDelegate PlayableDelegate
        {
            get
            {
                if(_isOverride)
                {
                    return _playableDelegate;
                }
                return _onGetParentRule?.Invoke(GetType());
            }
            private set => _playableDelegate = value;
        }

        public Rule(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Selects the rule to be executed based on the override status.
        /// </summary>
        /// <param name="playableFunc"></param>
        /// <param name="onGetParentRule"></param>
        /// <returns></returns>
        public PlayableDelegate SetPlayableRule(PlayableDelegate playableFunc, Func<Type, PlayableDelegate> onGetParentRule)
        {
            if (_isOverride)
            {
                PlayableDelegate = playableFunc;
                PlayableDelegate ??= _ => true;
                return PlayableDelegate;
            }

            _onGetParentRule = onGetParentRule;
            PlayableDelegate = _onGetParentRule?.Invoke(GetType());
            PlayableDelegate ??= _ => true;
            return playableFunc;
        }

        public static implicit operator T(Rule<T> property) => property == null ? default : property.Value;

        public static class NameOf
        {
            public const string IsOverride = nameof(_isOverride);
        }
    }
}