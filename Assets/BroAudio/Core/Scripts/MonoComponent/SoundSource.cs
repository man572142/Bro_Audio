using UnityEngine;
using UnityEngine.Serialization;

namespace Ami.BroAudio
{
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

        public void Play() => _currentPlayer = BroAudio.Play(_sound);
        public void Play(Transform followTarget) => _currentPlayer = BroAudio.Play(_sound, followTarget);
        public void Play(Vector3 positon) => _currentPlayer = BroAudio.Play(_sound, positon);
        public void Stop() => _currentPlayer?.Stop();
        public void Stop(float fadeTime) => _currentPlayer?.Stop(fadeTime);
        public void SetVolume(float vol) => _currentPlayer?.SetVolume(vol);
        public void SetVolume(float vol, float fadeTime) => _currentPlayer?.SetVolume(vol, fadeTime);
        public void SetPitch(float pitch) => _currentPlayer?.SetPitch(pitch);
        public void SetPitch(float pitch, float fadeTime) => _currentPlayer?.SetPitch(pitch, fadeTime);

        private IAudioPlayer _currentPlayer;

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
            if(_stopOnDisable && _currentPlayer != null && _currentPlayer.IsPlaying)
            {
                _currentPlayer.Stop(_overrideFadeOut);
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
