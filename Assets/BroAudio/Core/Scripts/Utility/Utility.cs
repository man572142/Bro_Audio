using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
		public static string ToName(this int id)
		{
			return SoundManager.Instance.GetNameByID(id);
		}

		public static T DecorateWith<T>(this AudioPlayer origin) where T : AudioPlayerDecorator, new()
		{
			if (origin != null)
			{
				T result = new T();
				result.Init(origin);
				return result;
			}
			return null;
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
					default:
						// todo:
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
                default:
					return default;
            }
        }
    }
}