using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class SparkleObject : InteractiveComponent
    {
        [SerializeField] SoundID _sound = default;

        protected override bool IsTriggerOnce => true;

        public override void OnInZoneChanged(bool isInZone)
        {
            base.OnInZoneChanged(isInZone);
            BroAudio.Play(_sound);
            Destroy(gameObject);
        }
    } 
}
