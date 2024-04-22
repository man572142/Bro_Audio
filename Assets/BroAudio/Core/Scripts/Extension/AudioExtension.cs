using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static Ami.Extension.AudioConstant;

namespace Ami.Extension
{
	public static class AudioExtension
	{
		public struct AudioClipSetting
		{
			public readonly int Frequency;
			public readonly int Channels;
			public readonly int Samples;
			public readonly bool Ambisonic;
			public readonly AudioClipLoadType LoadType;
			public readonly bool PreloadAudioData;
			public readonly bool LoadInBackground;
			public readonly AudioDataLoadState LoadState;

			public AudioClipSetting(AudioClip originClip, bool isMono)
			{
				Frequency = originClip.frequency;
				Channels = isMono ? 1 : originClip.channels;
				Samples = originClip.samples;
				Ambisonic = originClip.ambisonic;
				LoadType = originClip.loadType;
				PreloadAudioData = originClip.preloadAudioData;
				LoadInBackground = originClip.loadInBackground;
				LoadState = originClip.loadState;
			}
		}

		private const float SecondsPerMinute = 60f;

		public static float ToDecibel(this float vol, bool allowBoost = true)
		{
			return Mathf.Log10(vol.ClampNormalize(allowBoost)) * DefaultDecibelVolumeScale;
		}

		public static float ToNormalizeVolume(this float dB, bool allowBoost = true)
		{
			float maxVol = allowBoost ? MaxDecibelVolume : FullDecibelVolume;
			if (dB >= maxVol)
			{
				return allowBoost ? MaxVolume : FullVolume;
			}
			return Mathf.Pow(10, dB.ClampDecibel(allowBoost) / DefaultDecibelVolumeScale);
		}

		public static float ClampNormalize(this float vol, bool allowBoost = false)
		{
			return Mathf.Clamp(vol, MinVolume, allowBoost ? MaxVolume : FullVolume);
		}

		public static float ClampDecibel(this float dB, bool allowBoost = false)
		{
			return Mathf.Clamp(dB, MinDecibelVolume, allowBoost ? MaxDecibelVolume : FullDecibelVolume);
		}

		public static bool TryGetSampleData(this AudioClip originClip, out float[] sampleArray, float startPosInSecond, float endPosInSecond)
		{
			int dataSampleLength = GetDataSample(originClip, originClip.length - endPosInSecond - startPosInSecond);

			sampleArray = new float[dataSampleLength];
			bool sucess = originClip.GetData(sampleArray, GetTimeSample(originClip, startPosInSecond));

			if (!sucess)
			{
				Debug.LogError($"Can't get audio clip : {originClip.name} 's sample data!");
			}
			return sucess;
		}

		public static float[] GetSampleData(this AudioClip originClip, float startPosInSecond = 0f, float endPosInSecond = 0f)
		{
			if (TryGetSampleData(originClip, out var sampleArray, startPosInSecond, endPosInSecond))
			{
				return sampleArray;
			}
			return null;
		}

		public static AudioClip CreateAudioClip(string name, float[] samples, AudioClipSetting setting)
		{
			AudioClip result = AudioClip.Create(name, samples.Length / setting.Channels, setting.Channels, setting.Frequency, setting.LoadType == AudioClipLoadType.Streaming);
			result.SetData(samples, 0);
			return result;
		}

		public static int GetDataSample(AudioClip clip, float time, MidpointRounding rounding = MidpointRounding.AwayFromZero)
		{
			return (int)Math.Round(clip.frequency * clip.channels * time, rounding);
		}

		public static int GetTimeSample(AudioClip clip, float time, MidpointRounding rounding = MidpointRounding.AwayFromZero)
		{
			return (int)Math.Round(clip.frequency * time, rounding);
		}

		public static AudioClipSetting GetAudioClipSetting(this AudioClip audioClip, bool isMono = false)
		{
			return new AudioClipSetting(audioClip, isMono);
		}

		public static bool IsValidFrequency(float freq)
		{
			if (freq < MinFrequency || freq > MaxFrequency)
			{
				Debug.LogError($"The given frequency should be in {MinFrequency}Hz ~ {MaxFrequency}Hz.");
				return false;
			}
			return true;
		}

		public static float TempoToTime(float bpm, int beats)
		{
			if (bpm == 0)
			{
				return 0;
			}
			return SecondsPerMinute / bpm * beats;
		}

		public static void ChangeChannel(this AudioMixer mixer, string from, string to, float targetVol)
		{
			mixer.SafeSetFloat(from, MinDecibelVolume);
			mixer.SafeSetFloat(to, targetVol);
		}

		public static void SafeSetFloat(this AudioMixer mixer, string parameterName, float value)
		{
			if (!string.IsNullOrEmpty(parameterName))
			{
				mixer.SetFloat(parameterName, value);
			}
		}

		public static bool SafeGetFloat(this AudioMixer mixer, string parameterName, out float value)
		{
			value = default;
			if (!string.IsNullOrEmpty(parameterName))
			{
				return mixer.GetFloat(parameterName, out value);
			}
			return false;
		}
	}
}