using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class AudioReverbZoneNote : MonoBehaviour
    {
#pragma warning disable 414
        [SerializeField] GameObject _addtionalNote = null;
#pragma warning restore 414
        void Start()
        {
#if UNITY_2021
            _addtionalNote.SetActive(true); 
#endif
        }
    } 
}
