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
        [SerializeField] PositionMode _positionMode = default;

        public void Play() => CurrentPlayer = BroAudio.Play(_sound);
        public void Play(Transform followTarget) => CurrentPlayer = BroAudio.Play(_sound, followTarget);
        public void Play(Vector3 positon) => CurrentPlayer = BroAudio.Play(_sound, positon);
        public void Stop() => CurrentPlayer?.Stop();
        public void Stop(float fadeTime) => CurrentPlayer?.Stop(fadeTime);
        public void SetVolume(float vol) => CurrentPlayer?.SetVolume(vol);
        public void SetVolume(float vol, float fadeTime) => CurrentPlayer?.SetVolume(vol, fadeTime);
        public void SetPitch(float pitch) => CurrentPlayer?.SetPitch(pitch);
        public void SetPitch(float pitch, float fadeTime) => CurrentPlayer?.SetPitch(pitch, fadeTime);

        public IAudioPlayer CurrentPlayer { get; private set; }

        private void OnEnable()
        {
            if (!_playOnEnable)
            {
                return;
            }

            switch (_positionMode)
            {
                case PositionMode.Global:
                    Play();
                    break;
                case PositionMode.FollowGameObject:
                    Play(transform);
                    break;
                case PositionMode.StayHere:
                    Play(transform.position);
                    break;
            }

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

        public static class NameOf
        {
            public const string PlayOnEnable = nameof(_playOnEnable);
            public const string StopOnDisable = nameof(_stopOnDisable);
            public const string OnlyPlayOnce = nameof(_onlyPlayOnce);
            public const string OverrideFadeOut = nameof(_overrideFadeOut);
            public const string SoundID = nameof(_sound);
            public const string PositionMode = nameof(_positionMode);
        }
    }
}
