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

        public void Play() => BroAudio.Play(_sound);
        public void Play(Transform followTarget) => BroAudio.Play(_sound, followTarget);
        public void Play(Vector3 positon) => BroAudio.Play(_sound, positon);
        public void Stop() => BroAudio.Stop(_sound);
		public void Stop(float fadeTime) => BroAudio.Stop(_sound,fadeTime);
        public void SetVolume(float vol) => BroAudio.SetVolume(_sound,vol);


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
