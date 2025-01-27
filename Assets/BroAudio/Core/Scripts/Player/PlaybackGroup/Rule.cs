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
    public abstract class Rule<T> : IRule
    {
        public T Value;
        private PlayableDelegate _ruleDelegate;
        private Func<Type, IRule> _onGetParentRule;
        [SerializeField] private bool _isOverride = true;

        public PlayableDelegate RuleDelegate
        {
            get
            {
                if(_isOverride)
                {
                    return _ruleDelegate;
                }
                return _onGetParentRule?.Invoke(GetType())?.RuleDelegate;
            }
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
        public PlayableDelegate SetPlayableRule(PlayableDelegate playableFunc, Func<Type, IRule> onGetParentRule)
        {
            if (_isOverride)
            {
                _ruleDelegate = playableFunc;
                _ruleDelegate ??= _ => true;
                return _ruleDelegate;
            }

            _onGetParentRule = onGetParentRule;
            _ruleDelegate = _onGetParentRule?.Invoke(GetType())?.RuleDelegate;
            _ruleDelegate ??= _ => true;
            return _ruleDelegate;
        }

        public static implicit operator T(Rule<T> property) => property == null ? default : property.Value;

        public static class NameOf
        {
            public const string IsOverride = nameof(_isOverride);
        }
    }
}