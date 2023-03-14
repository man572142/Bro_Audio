using System;
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
            if(dB >= 0)
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

        public static AudioClip Trim(this AudioClip originClip, float startPos, float endPos,string newClipName)
        {
            int startSample = (int)(startPos * originClip.frequency);
            int sampleLength = (int)((originClip.length - endPos - startPos) * originClip.frequency);
            float[] sampleArray = new float[sampleLength];
            bool sucess = originClip.GetData(sampleArray, startSample);

            AudioClip resultClip = null;
            if (sucess)
            {
                resultClip = AudioClip.Create(newClipName, sampleLength, originClip.channels, originClip.frequency, originClip.loadType == AudioClipLoadType.Streaming);
                resultClip.SetData(sampleArray, 0);
            }

            return resultClip;
        }

        public static AudioClip Boost(this AudioClip originClip, float boostVol, string clipNameSuffix = "_Edited")
		{
            float[] sampleArray = new float[originClip.samples];
            originClip.GetData(sampleArray, 0);

            for(int i = 0; i < sampleArray.Length;i++)
			{
                int sign = 1;
                float vol = sampleArray[i];
                if(vol < 0)
				{
                    sign = -1;
                    vol *= sign;
				}

                float db = vol.ToDecibel();
                db += boostVol;

                sampleArray[i] = db.ToNormalizeVolume() * sign;
			}

            AudioClip resultClip = AudioClip.Create(originClip.name + clipNameSuffix, sampleArray.Length, originClip.channels, originClip.frequency, originClip.loadType == AudioClipLoadType.Streaming);
            resultClip.SetData(sampleArray, 0);
            return resultClip;
		}

        public static AudioClip Reverse(this AudioClip originClip,string clipNameSuffix = "_Edited")
		{
            float[] sampleArray = new float[originClip.samples];
            originClip.GetData(sampleArray, 0);

            Array.Reverse(sampleArray);

            AudioClip resultClip = AudioClip.Create(originClip.name + clipNameSuffix, sampleArray.Length, originClip.channels, originClip.frequency, originClip.loadType == AudioClipLoadType.Streaming);
            resultClip.SetData(sampleArray, 0);
            return resultClip;
        }
    }

}