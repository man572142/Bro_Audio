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

        public const float HaasEffectInSecond = 0.04f;

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

        public static bool TryGetSampleData(this AudioClip originClip,out float[] sampleArray, float startPosInSecond = 0f, float endPosInSecond = 0f)
        {
            int startSample = (int)(startPosInSecond * originClip.samples * originClip.channels);
            int sampleLength = (int)((originClip.length - endPosInSecond - startPosInSecond) * originClip.frequency * originClip.channels);
            
            sampleArray = new float[sampleLength];
            bool sucess = originClip.GetData(sampleArray, startSample);

            if(!sucess)
			{
                Debug.LogError($"Can't get audio clip : {originClip.name} 's sample data!");
			}
            return sucess;
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

        public static AudioClip Trim(this AudioClip originClip, float startPos, float endPos,string newClipName)
        {
            AudioClip resultClip = null;
            if (originClip.TryGetSampleData(out var sampleArray, startPos, endPos))
			{
                AudioClipSetting settings = originClip.GetAudioClipSetting();
                resultClip = CreateAudioClip(newClipName, sampleArray, settings);
            }

            return resultClip;
        }

        public static AudioClip Boost(this AudioClip originClip, float boostVolInDb, string newClipName)
		{
			if(!originClip.TryGetSampleData(out var sampleArray))
			{
                return null;
			}

			for (int i = 0; i < sampleArray.Length; i++)
			{
				int sign = 1;
				float vol = sampleArray[i];
				if (vol < 0)
				{
					sign = -1;
					vol *= sign;
				}

				float db = vol.ToDecibel();
				db += boostVolInDb;

				sampleArray[i] = db.ToNormalizeVolume() * sign;
			}
            AudioClipSetting setting = originClip.GetAudioClipSetting();
			return CreateAudioClip(newClipName, sampleArray, setting);
        }

		

		public static AudioClip Reverse(this AudioClip originClip,string newClipName)
		{
            if (!originClip.TryGetSampleData(out var sampleArray))
            {
                return null;
            }

            Array.Reverse(sampleArray);

            AudioClipSetting setting = originClip.GetAudioClipSetting();
            return CreateAudioClip(newClipName, sampleArray, setting);
        }

    }

}