using System;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Data
{
	public class SpatialSetting : ScriptableObject
	{
        public float StereoPan = AudioConstant.DefaultPanStereo;
        public float DopplerLevel = AudioConstant.DefaultDoppler;
        public float MinDistance = AudioConstant.AttenuationMinDistance;
        public float MaxDistance = AudioConstant.AttenuationMaxDistance;

        public AnimationCurve SpatialBlend;
        public AnimationCurve ReverbZoneMix;
        public AnimationCurve Spread;
        public AnimationCurve CustomRolloff;
        public AudioRolloffMode RolloffMode = AudioConstant.DefaultRolloffMode;
    }
}