using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio
{
    public static class AudioExtension
    {
        public const float MinDecibelVolume = -80f;
        public const float MaxDecibelVolume = 0f;

        public static float ToDecibel(this float vol)
        {
            return Mathf.Lerp(MinDecibelVolume, MaxDecibelVolume, Mathf.Clamp01(vol));
        }

        public static bool Validate(string typeName, int index,AudioClip clip , float startPosition, float fadeInTime = -1,float fadeOutTime = -1)
        {
            if (clip == null)
            {
                Debug.LogError($"[SoundSystem] Audio clip has not been assigned! please check element {index} in {typeName}.");
                return false;
            }
            float controlLength = (fadeInTime > 0f ? fadeInTime : 0f) + (fadeOutTime > 0f ? fadeOutTime : 0f) + startPosition;
            if (controlLength  > clip.length)
            {
                Debug.LogError($"[SoundSystem] Time control value should not greater than clip's length! please check element {index} in {typeName}.");
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
                    Debug.LogError($"[SoundSystem] sound clip in element {index} has not been assigned!");
                    return false;
                }
                if (startPosition > clip.length)
                {
                    Debug.LogError($"[SoundSystem] Time control value should not greater than clip's length! please check element {index} in {typeName}.");
                    return false;
                }
            }
            
            return true;
        }
    }

}