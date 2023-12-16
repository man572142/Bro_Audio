using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
	public static class AudioConstant
	{
		/// <summary>
		/// The minimum volume that Unity AudioMixer can reach in dB.
		/// </summary>
		public const float MinDecibelVolume = -80f;
		/// <summary>
		/// The full (or default) volume in dB.
		/// </summary>
		public const float FullDecibelVolume = 0f;
		/// <summary>
		/// The maximum volume that Unity AudioMixer can reach in dB.
		/// </summary>
		public const float MaxDecibelVolume = 20f;


		/// <summary>
		/// The normalized minimum volume that Unity AudioMixer can reach
		/// </summary>
		public const float MinVolume = 0.0001f;

		/// <summary>
		/// The normalized full (or default) volume
		/// </summary>
		public const float FullVolume = 1f;

		/// <summary>
		/// The normalized maximum volume that Unity AudioMixer can reach
		/// </summary>
		public const float MaxVolume = 10f;


		/// <summary>
		/// The maximum sound frequence in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
		/// </summary>
		public const float MaxFrequence = 22000f;

		/// <summary>
		/// The minimum sound frequence in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
		/// </summary>
		public const float MinFrequence = 10f;

        // Base on AuidoSource default values
		public const float DefaultDoppler = 1f;
		public const float AttenuationMinDistance = 1f;
        public const float AttenuationMaxDistance = 500f;
        public const float SpatialBlend_3D = 1f;
        public const float SpatialBlend_2D = 0f;
        public const float DefaultPitch = 1f; // The default pitch for both AudioSource and AudioMixer.
        public const float MinAudioSourcePitch = 0.1f; // todo: values under 0 is not supported currently. Might support in the future to achieve that reverse feature.
        public const float MaxAudioSourcePitch = 3f;
        public const float MinMixerPitch = 0.1f;
        public const float MaxMixerPitch = 10f;
        public const int DefaultPriority = 128;
        public const int MinPriority = 0;
        public const float MaxPriority = 256;
        public const float DefaultSpread = 0f;
        public const float DefaultReverZoneMix = 1f;

        public static float DecibelVoulumeFullScale => MaxDecibelVolume - MinDecibelVolume;
		public static AnimationCurve SpatialBlend => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve ReverbZoneMix => AnimationCurve.Constant(0f, 0f, 1f);
        public static AnimationCurve Spread => AnimationCurve.Constant(0f, 0f, 0f);
        public static AnimationCurve CustomRolloff => Logarithmic(AttenuationMinDistance / AttenuationMaxDistance, 1f,1f);

        #region From Unity Source Code
        /// A logarithmic curve starting at /timeStart/, /valueStart/ and ending at /timeEnd/, /valueEnd/
        private static AnimationCurve Logarithmic(float timeStart, float timeEnd, float logBase)
        {
            float value, slope, s;
            List<Keyframe> keys = new List<Keyframe>();
            // Just plain set the step to 2 always. It can't really be any less,
            // or the curvature will end up being imprecise in certain edge cases.
            float step = 2;
            timeStart = Mathf.Max(timeStart, 0.0001f);
            for (float d = timeStart; d < timeEnd; d *= step)
            {
                // Add key w. sensible tangents
                value = LogarithmicValue(d, timeStart, logBase);
                s = d / 50.0f;
                slope = (LogarithmicValue(d + s, timeStart, logBase) - LogarithmicValue(d - s, timeStart, logBase)) / (s * 2);
                keys.Add(new Keyframe(d, value, slope, slope));
            }

            // last key
            value = LogarithmicValue(timeEnd, timeStart, logBase);
            s = timeEnd / 50.0f;
            slope = (LogarithmicValue(timeEnd + s, timeStart, logBase) - LogarithmicValue(timeEnd - s, timeStart, logBase)) / (s * 2);
            keys.Add(new Keyframe(timeEnd, value, slope, slope));

            return new AnimationCurve(keys.ToArray());
        }

        private static float LogarithmicValue(float distance, float minDistance, float rolloffScale)
        {
            if ((distance > minDistance) && (rolloffScale != 1.0f))
            {
                distance -= minDistance;
                distance *= rolloffScale;
                distance += minDistance;
            }
            if (distance < .000001f)
                distance = .000001f;
            return minDistance / distance;
        }
        #endregion
    }
}