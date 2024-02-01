using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class PlayerSoundController : MonoBehaviour
    {
        [SerializeField] AudioID _footstep = default;

        public void OnFootstep()
        {
            BroAudio.Play(_footstep, transform.transform.position);
        }
    } 
}
