using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio
{
    /// <summary>
    /// A ruleset that determines how sounds that assigned to this group can be played.
    /// </summary>
    /// <remarks>
    /// Please override the <see cref="InitializeRules"/> method to specify the behavior of the rules
    /// </remarks>
    public abstract partial class PlaybackGroup : ScriptableObject, IPlayableValidator
    {
        public delegate bool IsPlayableDelegate(SoundID id);

        private PlaybackGroup _parent;
        private List<IRule> _rules = null;

        protected PlaybackGroup Parent
        {
            get
            {
                if(!_parent)
                {
                    _parent = Runtime.SoundManager.Instance.Setting.GlobalPlaybackGroup;
                }
                return _parent;
            }
        }

        /// <summary>
        /// Initializes the rules that determine how the sounds can be played.
        /// </summary>
        protected abstract IEnumerable<IRule> InitializeRules();

        /// <summary>
        /// Triggered when the player passes the playable validation and is about to play.
        /// </summary>
        public virtual void OnGetPlayer(IAudioPlayer player)
        {
        }

        /// <summary>
        /// Checks if the sound is playable.
        /// </summary>
        public bool IsPlayable(SoundID id)
        {
            _rules ??= new List<IRule>(InitializeRules());

            foreach (var rule in _rules)
            {
                bool isPlayable = rule.RuleMethod.Invoke(id);
                if(!isPlayable)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Sets the rule to be executed based on the override status.
        /// </summary>
        /// <param name="playableFunc"></param>
        /// <param name="onGetParentRule"></param>
        /// <returns></returns>
        protected IRule Initialize<T>(Rule<T> rule, IsPlayableDelegate ruleMethod)
        {
            return rule.Initialize(ruleMethod, Parent.GetRule);
        }

        internal IRule GetRule(Type ruleType)
        {
            _rules ??= new List<IRule>(InitializeRules());

            foreach (var rule in _rules)
            {
                if (rule.GetType() == ruleType)
                {
                    return rule;
                }
            }
            return new EmptyRule(ruleType);
        }

        internal void SetParent(PlaybackGroup parent)
        {
            _parent = parent;
        }
    }
}