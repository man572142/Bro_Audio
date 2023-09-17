using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Audio;
using static Ami.Extension.ReflectionExtension;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.Extension
{
	public static class BroAudioReflection
	{
		public const string SendExposeParameterSuffix = "_Send";
		public const string DefaultSnapshot = "Snapshot";
		public const string SendEffectParameter = "Send";
		public const string AttenuationEffectParameter = "Attenuation";

		public const string SendTargetProperty = "sendTarget";
		public const string ColorIndexParameter = "userColorIndex";
		public const string WetMixProperty = "enableWetMix";

		public static AudioMixerGroup DuplicateBroAudioTrack(AudioMixer mixer, AudioMixerGroup mainTrack, AudioMixerGroup sourceTrack, string trackName)
		{
			// Using [DuplicateGroupRecurse] method on AudioMixerController will cause some unexpected result.
			// Create a new one and copy the setting manually might be better.

			AudioClassReflectionHelper reflection = new AudioClassReflectionHelper();

			AudioMixerGroup newGroup = ExecuteMethod("CreateNewGroup", new object[] { trackName, false }, reflection.MixerClass, mixer) as AudioMixerGroup;
			if (newGroup != null)
			{
				ExecuteMethod("AddChildToParent", new object[] { newGroup, mainTrack }, reflection.MixerClass, mixer);
				ExecuteMethod("AddGroupToCurrentView", new object[] { newGroup }, reflection.MixerClass, mixer);
				ExecuteMethod("OnSubAssetChanged", null, reflection.MixerClass, mixer);
				CopyMixerGroupSetting(sourceTrack, newGroup, trackName, reflection);
			}
			return newGroup;
		}

		private static void CopyMixerGroupSetting(AudioMixerGroup source, AudioMixerGroup target, string trackName, AudioClassReflectionHelper reflection)
		{
			CopyColorIndex(source, target, reflection);
			SetValueForVolume(target.audioMixer, reflection, target);

			if (TryGetGUIDForVolume(reflection.MixerGroupClass, target, out GUID volGUID))
			{
				object volParaPath = CreateParameterPathInstance("AudioGroupParameterPath", target, volGUID);
				CustomParameterExposer.AddExposedParameter(trackName, volParaPath, volGUID, target.audioMixer, reflection);
			}

			object effect = CopySendEffect(source, target, reflection);
			if (TryGetGUIDForMixLevel(reflection.EffectClass, effect, out GUID effectGUID))
			{
				object effectParaPath = CreateParameterPathInstance("AudioEffectParameterPath", target, effect, effectGUID);
				CustomParameterExposer.AddExposedParameter(trackName + SendExposeParameterSuffix, effectParaPath, effectGUID, target.audioMixer, reflection);
			}
		}

		private static object CreateParameterPathInstance(string className, params object[] parameters)
		{
			Type type = AudioClassReflectionHelper.GetUnityAudioEditorClass(className);
			return CreateNewObjectWithReflection(type, parameters);
		}

		private static void CopyColorIndex(AudioMixerGroup sourceGroup, AudioMixerGroup targetGroup, AudioClassReflectionHelper reflection)
		{
			int colorIndex = GetProperty<int>(ColorIndexParameter,reflection.MixerGroupClass, sourceGroup);
			SetProperty(ColorIndexParameter, reflection.MixerGroupClass, targetGroup, colorIndex);
		}

		private static object CopySendEffect(AudioMixerGroup sourceGroup, AudioMixerGroup targetGroup, AudioClassReflectionHelper reflection)
		{
			if (TryGetEffect(sourceGroup, SendEffectParameter, reflection,out object sourceSendEffect))
			{
				var sendTarget = GetProperty<object>(SendTargetProperty, reflection.EffectClass, sourceSendEffect);
				var clonedEffect = ExecuteMethod("CopyEffect", new object[] { sourceSendEffect }, reflection.MixerClass, sourceGroup.audioMixer);
				SetProperty(SendTargetProperty, reflection.EffectClass, clonedEffect, sendTarget);
				SetProperty(WetMixProperty, reflection.EffectClass, clonedEffect, true);
				ExecuteMethod("InsertEffect", new object[] { clonedEffect, 0 }, reflection.MixerGroupClass, targetGroup);
				return clonedEffect;
			}
			return null;
		}

		private static bool TryGetEffect(AudioMixerGroup mixerGroup,string targetEffectName ,AudioClassReflectionHelper reflection,out object result)
		{
			result = null;
			object[] effects = GetProperty<object[]>("effects", reflection.MixerGroupClass, mixerGroup);

			foreach (var effect in effects)
			{
				string effectName = GetProperty<string>("effectName", reflection.EffectClass, effect);
				if (effectName == targetEffectName)
				{
					result = effect;
				}
			}
			return result != null;
		}

		private static void SetValueForVolume(AudioMixer mixer, AudioClassReflectionHelper reflection, AudioMixerGroup newGroup)
		{
			var snapshot = mixer.FindSnapshot(DefaultSnapshot);
			ExecuteMethod("SetValueForVolume", new object[] { mixer, snapshot, AudioConstant.MinDecibelVolume }, reflection.MixerGroupClass, newGroup);
		}

		private static bool TryGetGUIDForVolume(Type mixerGroupClass,AudioMixerGroup mixerGroup,out GUID guid)
		{
			guid = default;
			object obj = ExecuteMethod("GetGUIDForVolume", ReflectionExtension.Void, mixerGroupClass, mixerGroup);
			return TryConvertGUID(obj, ref guid);
		}
		private static bool TryGetGUIDForMixLevel(Type effectClass, object effect, out GUID guid)
		{
			guid = default;
			object obj = ExecuteMethod("GetGUIDForMixLevel", ReflectionExtension.Void, effectClass, effect);
			return TryConvertGUID(obj, ref guid);
		}

		private static bool TryConvertGUID(object obj, ref GUID guid)
		{
			try
			{
				guid = (GUID)obj;
			}
			catch (InvalidCastException)
			{
				LogError($"Cast GUID failed! object :{obj}");
			}
			return guid != default && !guid.Empty();
		}
	}
}