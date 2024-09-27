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
        public const float MinAudioSourcePitch = -3f;
        public const float MaxAudioSourcePitch = 3f;
        public const float MinPlayablePitch = 0.01f;
        public const float MaxMixerPitch = 10f;
        public const int DefaultPriority = 128;
        public const int HighestPriority = 0;
        public const float LowestPriority = 256;
        public const float DefaultSpread = 0f;
        public const float DefaultReverZoneMix = 1f;
        public const float DefaultPanStereo = 0f;
        public const AudioRolloffMode DefaultRolloffMode = AudioRolloffMode.Logarithmic;

        public const float MinLogValue = -4; // Log10(MinVolume) => Log10(0.0001) 
        public const float MaxLogValue = 1; // Log10(MaxVolume) => Log10(10)
        public const float FullVolumeLogValue = 0; // Log10(FullVolume) => Log10(1);

        public const float MinFrequencyLogValue = 1; // Log10(MinFrequency) => Log10(10) 
        public const float MaxFrequencyLogValue = 4.34242268f; // Log10(MinFrequency) => Log10(22000) 

        public static float DecibelVoulumeFullScale => MaxDecibelVolume - MinDecibelVolume;
    }
}