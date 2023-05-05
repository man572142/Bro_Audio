using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.Extension
{
    public static class AudioExtension
    {
        public struct AudioClipSetting
		{
            public readonly float Length;
            public readonly int Frequency;
            public readonly int Channels;
            public readonly int Samples;
            public readonly bool Ambisonic;
            public readonly AudioClipLoadType LoadType;
            public readonly bool PreloadAudioData;
            public readonly bool LoadInBackground;
            public readonly AudioDataLoadState LoadState;

			public AudioClipSetting(AudioClip originClip)
			{
				Length = originClip.length;
				Frequency = originClip.frequency;
				Channels = originClip.channels;
				Samples = originClip.samples;
				Ambisonic = originClip.ambisonic;
				LoadType = originClip.loadType;
				PreloadAudioData = originClip.preloadAudioData;
				LoadInBackground = originClip.loadInBackground;
				LoadState = originClip.loadState;
			}
		}

        public const float HaasEffectInSeconds = 0.04f;

        public const float MinDecibelVolume = -80f;
        public const float MaxDecibelVolume = 0f;
        public const float MaxBoostDecibelVolume = 20f;

        public const float MinVolume = 0.0001f;
        public const float MaxVolume = 1f;
        public const float MaxBoostVolume = 10f;

        public static float ToDecibel(this float vol)
        {  
            return Mathf.Log10(vol.ClampNormalize(true)) * 20f;
        }

        public static float ToNormalizeVolume(this float dB,bool allowBoost = false)
        {
            float maxVol = allowBoost ? MaxBoostDecibelVolume : MaxDecibelVolume;
            if(dB >= maxVol)
            {
                return allowBoost ? MaxBoostVolume : MaxVolume;
            }
            return Mathf.Pow(10, dB.ClampDecibel(allowBoost) / 20f);
        }

        public static float ClampNormalize(this float vol, bool allowBoost = false)
        {
            return Mathf.Clamp(vol, MinVolume, allowBoost? MaxBoostVolume : MaxVolume);
        }

        public static float ClampDecibel(this float dB, bool allowBoost = false)
        {
            return Mathf.Clamp(dB,MinDecibelVolume,allowBoost? MaxBoostDecibelVolume : MaxDecibelVolume);
        }

        public static bool TryGetSampleData(this AudioClip originClip,out float[] sampleArray, float startPosInSecond = 0f, float endPosInSecond = 0f)
        {
            int startSample = (int)(startPosInSecond * originClip.frequency * originClip.channels);
            int sampleLength = (int)((originClip.length - endPosInSecond - startPosInSecond) * originClip.frequency * originClip.channels);
            
            sampleArray = new float[sampleLength];
            bool sucess = originClip.GetData(sampleArray, startSample);

            if(!sucess)
			{
                Debug.LogError($"Can't get audio clip : {originClip.name} 's sample data!");
			}
            return sucess;
        }

        public static float[] GetSampleData(this AudioClip originClip, float startPosInSecond = 0f, float endPosInSecond = 0f)
		{
            if(TryGetSampleData(originClip,out var sampleArray,startPosInSecond,endPosInSecond))
			{
                return sampleArray;
			}
            return null;
		}

        public static AudioClip CreateAudioClip(string name,float[] samples,AudioClipSetting setting)
		{
            AudioClip result = AudioClip.Create(name, samples.Length, setting.Channels, setting.Frequency, setting.LoadType == AudioClipLoadType.Streaming);
            result.SetData(samples, 0);
            return result;
        }

        public static AudioClipSetting GetAudioClipSetting(this AudioClip audioClip)
		{
            return new AudioClipSetting(audioClip);
		}
    }
}