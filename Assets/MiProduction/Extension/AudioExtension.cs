using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.Extension
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

        public static AudioClip Trim(this AudioClip originClip, float startPos, float endPos,string clipNameSuffix = "_Edited")
        {
            int startSample = (int)(startPos * originClip.frequency);
            int sampleLength = (int)((originClip.length - endPos - startPos) * originClip.frequency);
            float[] sampleArray = new float[sampleLength];
            bool sucess = originClip.GetData(sampleArray, startSample);

            AudioClip resultClip = null;
            if (sucess)
            {
                resultClip = AudioClip.Create(originClip.name + clipNameSuffix, sampleLength, originClip.channels, originClip.frequency, originClip.loadType == AudioClipLoadType.Streaming);
                resultClip.SetData(sampleArray, 0);
            }

            return resultClip;
        }
    }

}