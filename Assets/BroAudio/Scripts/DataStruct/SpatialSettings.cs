using System;
using UnityEngine;

namespace Ami.BroAudio.Data
{
    [Serializable]
	public struct SpatialSettings
	{
        public float StereoPan;
        public float DopplerLevel;  
        public float MinDistance;
        public float MaxDistance;

        public AnimationCurve SpatialBlend;
        public AnimationCurve ReverbZoneMix;
        public AnimationCurve Spread;
        public AnimationCurve CustomRolloff;
        public AudioRolloffMode RolloffMode;

        public bool Equals(SpatialSettings other)
        {
            return StereoPan == other.StereoPan &&
                DopplerLevel == other.DopplerLevel &&
                MinDistance == other.MinDistance &&
                MaxDistance == other.MaxDistance &&
                SpatialBlend == other.SpatialBlend &&
                ReverbZoneMix == other.ReverbZoneMix &&
                Spread == other.Spread &&
                CustomRolloff == other.CustomRolloff;
        }

        public override bool Equals(object obj)
        {
            return obj is SpatialSettings settings && Equals(settings);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(SpatialSettings a, SpatialSettings b) => a.Equals(b);
        public static bool operator !=(SpatialSettings a, SpatialSettings b) => !a.Equals(b);
    }
}