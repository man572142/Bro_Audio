using System;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class AudioFilterReader : MonoBehaviour
    {
        public Action<float[], int> OnTriggerAudioFilterRead;

        private void OnAudioFilterRead(float[] data, int channels)
        {
            OnTriggerAudioFilterRead?.Invoke(data, channels);
        }
    } 
}