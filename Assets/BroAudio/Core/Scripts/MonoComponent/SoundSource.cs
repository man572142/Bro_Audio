using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		[SerializeField] bool _playOnStart = true;
		[SerializeField] SoundID _sound = default;
        [SerializeField] PositionMode _positionMode = default;

        public void Play() => _currentPlayer = BroAudio.Play(_sound);
        public void Play(Transform followTarget) => _currentPlayer = BroAudio.Play(_sound, followTarget);
        public void Play(Vector3 positon) => _currentPlayer = BroAudio.Play(_sound, positon);
        public void Stop() =>  _currentPlayer?.Stop();
		public void Stop(float fadeTime) => _currentPlayer?.Stop(fadeTime);
        public void SetVolume(float vol) => _currentPlayer?.SetVolume(vol);
        public void SetVolume(float vol, float fadeTime) => _currentPlayer?.SetVolume(vol, fadeTime);

        private IAudioPlayer _currentPlayer;


		private void Start()
		{
            if(!_playOnStart)
            {
                return;
            }

            switch(_positionMode)
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
		}
	}
}
