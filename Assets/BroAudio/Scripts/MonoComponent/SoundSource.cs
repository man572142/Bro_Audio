using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio
{
    [AddComponentMenu("BroAudio/" + nameof(SoundSource))]
    public class SoundSource : MonoBehaviour
    {
        [SerializeField] AudioID _sound = default;

        public void Play() => BroAudio.Play(_sound);
        public void Play(Transform followTarget) => BroAudio.Play(_sound, followTarget);
        public void Play(Vector3 positon) => BroAudio.Play(_sound, positon);

    }
}
