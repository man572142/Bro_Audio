using System;
using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;

namespace Ami.BroAudio
{
    public abstract class PlaybackGroup : ScriptableObject, IPlayableValidator
    {
        public delegate bool PlayableDelegate(SoundID id);

        [Serializable]
        public class Rule<T>
        {
            public T Value;
            [SerializeField] private bool _isOverride = true;

            public Rule(T value)
            {
                Value = value;
            }

            public PlayableDelegate Initialize(PlayableDelegate playableFunc, PlayableDelegate defaultPlayableFunc = null)
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

            public static implicit operator T(Rule<T> property)
            {
                return property == null ? default : property.Value;
            }

            public static class NameOf
            {
                public const string IsOverride = nameof(_isOverride);
            }
        }

        public static PlaybackGroup DefaultGroup => SoundManager.Instance.Setting.DefaultPlaybackGroup;

        private List<PlayableDelegate> _rules = null;

        public abstract IEnumerable<PlayableDelegate> InitializeRules();

        public virtual void HandlePlayer(IAudioPlayer player)
        {
        }

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