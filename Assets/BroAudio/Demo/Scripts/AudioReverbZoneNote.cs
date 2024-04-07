using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class AudioReverbZoneNote : MonoBehaviour
    {
        [SerializeField] GameObject _addtionalNote = null;
        void Start()
        {
#if UNITY_2021
            _addtionalNote.SetActive(true); 
#endif
        }
    } 
}
