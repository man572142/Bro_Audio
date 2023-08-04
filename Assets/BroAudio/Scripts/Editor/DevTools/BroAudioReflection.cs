using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
using static Ami.Extension.ReflectionExtension;

namespace Ami.Extension
{
	public static class BroAudioReflection
	{
		public const string LogTitle = "[BroAudio_DevTool]";
		public const string MainTrackName = "Main";
		public const string GenericTrackName = "Track";
		public const string DefaultSnapshot = "Snapshot";
		public const string SendEffectParameter = "Send";
		public const string AttenuationEffectParameter = "Attenuation";
		public const float MinDecibelVolume = -80f;
		public const string SendTargetParameter = "sendTarget";
		public const string ColorIndexParameter = "userColorIndex";
		public const string WetMixParameter = "enableWetMix";

		public static AudioMixerGroup DuplicateBroAudioMixerGroup(AudioMixer mixer)
		{
			// Using [DuplicateGroupRecurse] method on AudioMixerController will cause some unexpected result. (at least i can't solve)
			// It's safer to create a new one and copy the setting manually.

			AudioMixerGroup mainTrack = mixer.FindMatchingGroups(MainTrackName)?.FirstOrDefault();
			AudioMixerGroup[] tracks = mixer.FindMatchingGroups(GenericTrackName);
			int tracksCount = tracks.Length;
			if(mainTrack == default || tracks == default)
			{
				Debug.LogError($"{LogTitle} Create new mixer group is failed");
				return null;
			}

			AudioClassReflectionHelper reflection = new AudioClassReflectionHelper();
			string trackName = GenericTrackName + $"{tracksCount + 1}";

			AudioMixerGroup newGroup = ExecuteMethod("CreateNewGroup", new object[] { trackName, false }, reflection.MixerClass, mixer) as AudioMixerGroup;
			if (newGroup != null)
			{
				ExecuteMethod("AddChildToParent", new object[] { newGroup, mainTrack }, reflection.MixerClass, mixer);
				ExecuteMethod("AddGroupToCurrentView", new object[] { newGroup }, reflection.MixerClass, mixer);
				ExecuteMethod("OnSubAssetChanged", null, reflection.MixerClass, mixer);

				SetValueForVolume(mixer, reflection, newGroup);

				CopyColorIndex(tracks.Last(), newGroup, reflection);
				CopySendEffect(tracks.Last(), newGroup, reflection);
			}
			return newGroup;
		}

		private static void CopyColorIndex(AudioMixerGroup sourceGroup, AudioMixerGroup targetGroup, AudioClassReflectionHelper reflection)
		{
			int colorIndex = GetProperty<int>(ColorIndexParameter,reflection.MixerGroupClass, sourceGroup);
			SetProperty(ColorIndexParameter, reflection.MixerGroupClass, targetGroup, colorIndex);
		}

		public static void CopySendEffect(AudioMixerGroup sourceGroup, AudioMixerGroup targetGroup, AudioClassReflectionHelper reflection)
		{
			if (TryGetEffect(sourceGroup, SendEffectParameter, reflection,out object sourceSendEffect))
			{
				var sendTarget = GetProperty<object>(SendTargetParameter, reflection.EffectClass, sourceSendEffect);
				var clonedEffect = ExecuteMethod("CopyEffect", new object[] { sourceSendEffect }, reflection.MixerClass, sourceGroup.audioMixer);
				SetSendTarget(clonedEffect, sendTarget, reflection.EffectClass);
				EnableSendWetMix(clonedEffect, reflection.EffectClass);
				ExecuteMethod("InsertEffect", new object[] { clonedEffect, 0 }, reflection.MixerGroupClass, targetGroup);
			}
		}

		private static bool TryGetEffect(AudioMixerGroup mixerGroup,string targetEffectName ,AudioClassReflectionHelper reflection,out object result)
		{
			result = null;
			object[] effects = GetAllEffects(reflection.MixerGroupClass, mixerGroup);

			foreach (var effect in effects)
			{
				string effectName = GetEffectName(reflection.EffectClass, effect);
				if (effectName == targetEffectName)
				{
					result = effect;
				}
			}
			return result != null;
		}

		private static void EnableSendWetMix(object sendEffect,Type effectClass)
		{
			SetProperty(WetMixParameter, effectClass, sendEffect, true);
		}

		private static void SetSendTarget(object sendEffect, object value,Type effectClass)
		{
			SetProperty(SendTargetParameter, effectClass, sendEffect, value);
		}

		private static object[] GetAllEffects(Type mixerGroupClass, AudioMixerGroup mixerGroup)
		{
			return GetProperty<object[]>("effects", mixerGroupClass, mixerGroup);
		}

		private static string GetEffectName(Type effectClass, object effect)
		{
			return GetProperty<string>("effectName", effectClass, effect);
		}

		private static void SetValueForVolume(AudioMixer mixer, AudioClassReflectionHelper reflection, AudioMixerGroup newGroup)
		{
			var snapshot = mixer.FindSnapshot(DefaultSnapshot);
			ExecuteMethod("SetValueForVolume", new object[] { mixer, snapshot, MinDecibelVolume }, reflection.MixerGroupClass, newGroup);
		}

		private static void SetValueForMixLevel(Type effectClass, object effect, AudioMixer mixer, float value)
		{
			var snapshot = mixer.FindSnapshot(DefaultSnapshot);
			ExecuteMethod("SetValueForMixLevel", new object[] { mixer, snapshot, value }, effectClass, effect);
		}

		private static void SetValueForParameter(Type effectClass, object effect, AudioMixer mixer, string parameterName,float value)
		{
			var snapshot = mixer.FindSnapshot(DefaultSnapshot);
			ExecuteMethod("SetValueForParameter", new object[] { mixer, snapshot, parameterName,value }, effectClass, effect);
		}
	} 
}