using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MiProduction.BroAudio.Utility;

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
            //vol = Mathf.Clamp(vol, MinVolume, MaxVolume);
            //Debug.Log("toDb:" + vol.ToString());
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

        public static bool Validate(string typeName, int index,AudioClip clip ,int id ,float startPosition, float fadeInTime = -1,float fadeOutTime = -1)
        {
            if(id <= 0)
			{
                //本來就會有0,不用警告
                //Debug.LogError($"[SoundSystem] There is an invalid ID ! please update AudioLibraryAsset.");
                return false;
            }
            if (clip == null)
            {
                LogError($"Audio clip has not been assigned! please check element {index} in {typeName}.");
                return false;
            }
            float controlLength = (fadeInTime > 0f ? fadeInTime : 0f) + (fadeOutTime > 0f ? fadeOutTime : 0f) + startPosition;
            if (controlLength  > clip.length)
            {
                LogError($"Time control value should not greater than clip's length! please check element {index} in {typeName}.");
                return false;
            }
            return true;
        }

        public static bool Validate(string typeName, int index, AudioClip[] clips, float startPosition)
        {
            foreach(AudioClip clip in clips)
            {
                if (clip == null)
                {
                    LogError($"sound clip in element {index} has not been assigned!");
                    return false;
                }
                if (startPosition > clip.length)
                {
                    LogError($"Time control value should not greater than clip's length! please check element {index} in {typeName}.");
                    return false;
                }
            }
            
            return true;
        }
    }

}