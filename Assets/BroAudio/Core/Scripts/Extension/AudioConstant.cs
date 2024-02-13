using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
    public static class AudioConstant
    {
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


        public const float DefaultDecibelVolumeScale = 20f;

        /// <summary>
        /// The minimum volume that Unity AudioMixer can reach in dB.
        /// </summary>
        public const float MinDecibelVolume = -80f;  // DefaultDecibelVolumeScale * Log10(MinVolume)
        /// <summary>
        /// The full (or default) volume in dB.
        /// </summary>
        public const float FullDecibelVolume = 0f; // DefaultDecibelVolumeScale * Log10(FullVolume)
        /// <summary>
        /// The maximum volume that Unity AudioMixer can reach in dB.
        /// </summary>
        public const float MaxDecibelVolume = 20f;  // DefaultDecibelVolumeScale * Log10(MaxVolume)

        /// <summary>
        /// The maximum sound frequency in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
        /// </summary>
        public const float MaxFrequency = 22000f;

        /// <summary>
        /// The minimum sound frequency in Hz. (base on Unity's audio mixer effect like Lowpass/Highpass)
        /// </summary>
        public const float MinFrequency = 10f;

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
        public const int HighestPriority = 0;
        public const float LowestPriority = 256;
        public const float DefaultSpread = 0f;
        public const float DefaultReverZoneMix = 1f;

        public static float DecibelVoulumeFullScale => MaxDecibelVolume - MinDecibelVolume;
    }
}