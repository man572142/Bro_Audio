using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
        public const string LogTitle = "<b><color=#F3E9D7>[BroAudio] </color></b>";

        public static string ToName(this int id)
		{
			return SoundManager.Instance.GetNameByID(id);
		}

		#region Efficient HasFlag
		// faster than Enum.HasFlag, could be used in runtime.
		public static bool Contains(this BroAudioType flags, BroAudioType targetFlag)
		{
			return ((int)flags & (int)targetFlag) != 0;
		}

		public static bool Contains(this RandomFlags flags, RandomFlags targetFlag)
		{
			return ((int)flags & (int)targetFlag) != 0;
		}
		#endregion

		public static bool IsDefaultCurve(this AnimationCurve curve , float defaultValue)
		{
			if(curve == null || curve.length == 0)
			{
				return true;
			}
			else if(curve.length == 1 && curve[0].value == defaultValue)
			{
				return true;
			}
			return false;
		}

		public static void SetCustomCurveOrResetDefault(this AudioSource audioSource, AnimationCurve curve, AudioSourceCurveType curveType)
		{
			if(curveType == AudioSourceCurveType.CustomRolloff)
			{
				Debug.LogError(LogTitle + $"Don't use this method on {AudioSourceCurveType.CustomRolloff}, please use RolloffMode to detect if is default or not");
				return;
			}

			float defaultValue = GetCurveDefaultValue(curveType);

            if (!curve.IsDefaultCurve(defaultValue))
			{
				audioSource.SetCustomCurve(curveType,curve);
			}
			else
			{
                switch (curveType)
                {
                    case AudioSourceCurveType.SpatialBlend:
						audioSource.spatialBlend = defaultValue;
                        break;
                    case AudioSourceCurveType.ReverbZoneMix:
						audioSource.reverbZoneMix = defaultValue;
                        break;
                    case AudioSourceCurveType.Spread:
						audioSource.spread = defaultValue;
                        break;
				}
            }
        }

		private static float GetCurveDefaultValue(AudioSourceCurveType curveType)
		{
            switch (curveType)
            {
                case AudioSourceCurveType.SpatialBlend:
                    return AudioConstant.SpatialBlend_2D;
                case AudioSourceCurveType.ReverbZoneMix:
                    return AudioConstant.DefaultReverZoneMix;
                case AudioSourceCurveType.Spread:
                    return AudioConstant.DefaultSpread;
            }
			return default;
        }
    }
}