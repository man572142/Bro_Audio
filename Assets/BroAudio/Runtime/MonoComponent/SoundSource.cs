using UnityEngine;
using UnityEngine.Serialization;

namespace Ami.BroAudio
{
    [HelpURL("https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/sound-source")]
    [AddComponentMenu("BroAudio/" + nameof(SoundSource))]
    public partial class SoundSource : MonoBehaviour
    {
        public enum PositionMode
        {
            Global,
            FollowGameObject,
            StayHere,
        }

        [SerializeField] protected bool _onlyPlayOnce = false;
        [SerializeField, FormerlySerializedAs("_playOnStart")] protected bool _playOnEnable = false;
        [SerializeField] protected bool _stopOnDisable = false;
        [SerializeField] protected float _overrideFadeOut = -1f;
        [Space]
        [SerializeField] protected SoundID _sound = default;
        [SerializeField] protected PlaybackGroup _overrideGroup = null;
        [SerializeField] protected PositionMode _positionMode = default;
        [SerializeField] protected float _delay = 0f;

        public virtual IAudioPlayer CurrentPlayer { get; private set; }

        ///<inheritdoc cref="IAudioPlayer.IsPlaying()"/>
        public virtual bool IsPlaying => CurrentPlayer != null && CurrentPlayer.IsPlaying;

        ///<inheritdoc cref="IAudioPlayer.IsActive()"/>
        public virtual bool IsActive => CurrentPlayer != null && CurrentPlayer.IsActive;


        protected virtual void OnEnable()
        {
            if (!_playOnEnable)
            {
                return;
            }

            Play();
            if (_delay > 0)
            {
                CurrentPlayer.SetDelay(_delay);
            }

            if (_onlyPlayOnce)
            {
                _playOnEnable = false;
            }
        }

        protected virtual void OnDisable()
        {
            if(_stopOnDisable && CurrentPlayer != null && CurrentPlayer.IsPlaying)
            {
                CurrentPlayer.Stop(_overrideFadeOut);
            }
        }

#if UNITY_EDITOR
        public static class NameOf
        {
            public const string PlayOnEnable = nameof(_playOnEnable);
            public const string StopOnDisable = nameof(_stopOnDisable);
            public const string OnlyPlayOnce = nameof(_onlyPlayOnce);
            public const string OverrideFadeOut = nameof(_overrideFadeOut);
            public const string SoundID = nameof(_sound);
            public const string PositionModeProperty = nameof(_positionMode);
            public const string OverrideGroup = nameof(_overrideGroup);
            public const string Delay = nameof(_delay);
        } 
#endif
    }
}
