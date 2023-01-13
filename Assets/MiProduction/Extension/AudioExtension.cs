using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
    public static class AudioExtension
    {
        public const float MinDecibelVolume = -80f;
        public const float MaxDecibelVolume = 0f;

        public const float MinVolume = 0.0001f;
        public const float MaxVolume = 1f;

        public static float ToDecibel(this float vol)
        {  
            return Mathf.Log10(vol.ClampNormalize()) * 20f;
        }

        public static float ToNormalizeVolume(this float dB)
        {
            if(dB == 0)
            {
                return 1;
            }
            return Mathf.Pow(10, dB.ClampDecibel() / 20f);
        }

        public static float ClampNormalize(this float vol)
        {
            return Mathf.Clamp(vol, MinVolume, MaxVolume);
        }

        public static float ClampDecibel(this float dB)
        {
            return Mathf.Clamp(dB,MinDecibelVolume, MaxDecibelVolume);
        }

    }

}