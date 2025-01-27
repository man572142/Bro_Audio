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
        public delegate bool PlayableDelegate(SoundID id);

        private PlaybackGroup _parent;
        private List<PlayableDelegate> _rules = null;

        protected PlaybackGroup Parent
        {
            get
            {
                if(!_parent)
                {
                    return Runtime.SoundManager.Instance.Setting.GlobalPlaybackGroup;
                }
                return _parent;
            }
        }
        /// <summary>
        /// Initializes the rules that determine how the sounds can be played.
        /// </summary>
        public abstract IEnumerable<PlayableDelegate> InitializeRules();
        public abstract PlayableDelegate GetRule(Type ruleType);

        public void SetParent(PlaybackGroup parent)
        {
            _parent = parent;
        }

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