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

        [SerializeField] bool _onlyPlayOnce = false;
        [SerializeField, FormerlySerializedAs("_playOnStart")] bool _playOnEnable = true;
        [SerializeField] bool _stopOnDisable = false;
        [SerializeField] float _overrideFadeOut = -1f;
        [Space]
        [SerializeField] SoundID _sound = default;
        [SerializeField] PlaybackGroup _overrideGroup = null;
        [SerializeField] PositionMode _positionMode = default;

        public IAudioPlayer CurrentPlayer { get; private set; }

        ///<inheritdoc cref="IAudioPlayer.IsPlaying()"/>
        public bool IsPlaying => CurrentPlayer != null && CurrentPlayer.IsPlaying;

        ///<inheritdoc cref="IAudioPlayer.IsActive()"/>
        public bool IsActive => CurrentPlayer != null && CurrentPlayer.IsActive;


        private void OnEnable()
        {
            if (!_playOnEnable)
            {
                return;
            }

            Play();

            if (_onlyPlayOnce)
            {
                _playOnEnable = false;
            }
        }

        private void OnDisable()
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
            public const string PositionMode = nameof(_positionMode);
            public const string OverrideGroup = nameof(_overrideGroup);
        } 
#endif
    }
}
