using Ami.BroAudio.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ami.BroAudio
{
    [HelpURL("https://man572142s-organization.gitbook.io/broaudio/core-features/no-code-components/sound-source")]
    [AddComponentMenu("BroAudio/" + nameof(SoundSource))]
    public class SoundSource : MonoBehaviour
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
        public bool IsPlaying => CurrentPlayer != null && CurrentPlayer.IsPlaying;

        ///<inheritdoc cref="BroAudio.Play(SoundID)"/>
        public void PlayGlobally() => CurrentPlayer = BroAudio.Play(_sound, _overrideGroup);

        ///<inheritdoc cref="BroAudio.Play(SoundID, Transform)"/>
        public void Play(Transform followTarget) => CurrentPlayer = BroAudio.Play(_sound, followTarget, _overrideGroup);

        ///<inheritdoc cref="Play(SoundID, Vector3)"/>
        public void Play(Vector3 positon) => CurrentPlayer = BroAudio.Play(_sound, positon, _overrideGroup);

        /// <summary>
        /// Plays the audio base on the current PositionMode 
        /// </summary>
        public void Play()
        {
            switch (_positionMode)
            {
                case PositionMode.Global:
                    PlayGlobally();
                    break;
                case PositionMode.FollowGameObject:
                    Play(transform);
                    break;
                case PositionMode.StayHere:
                    Play(transform.position);
                    break;
            }
        }

        public void Stop() => Stop(AudioPlayer.UseEntitySetting);
        public void Stop(float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.Stop(fadeTime);
            }
        }

        public void SetVolume(float vol) => SetVolume(vol, BroAdvice.FadeTime_Immediate);
        public void SetVolume(float vol, float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.SetVolume(vol, fadeTime);
            }
        }

        public void SetPitch(float pitch) => SetPitch(pitch, BroAdvice.FadeTime_Immediate);
        public void SetPitch(float pitch, float fadeTime)
        {
            if (IsPlaying)
            {
                CurrentPlayer.SetPitch(pitch, fadeTime);
            }
        }

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
        } 
#endif
    }
}
