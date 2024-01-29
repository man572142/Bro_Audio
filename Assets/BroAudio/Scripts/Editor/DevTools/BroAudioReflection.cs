using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Audio;
using static Ami.Extension.ReflectionExtension;
using static Ami.BroAudio.Tools.BroLog;
using Ami.BroAudio.Tools;

namespace Ami.Extension.Reflection
{
	public static partial class BroAudioReflection
	{
		public enum MethodName
		{
			None,
			GetGUIDForVolume,
			GetGUIDForPitch,
			GetGUIDForMixLevel,
			GetValueForVolume,
			GetValueForPitch,
			SetValueForVolume,
			SetValueForPitch,
		}

		public const string DefaultSnapshot = "Snapshot";
		public const string SendEffectParameter = "Send";
		public const string AttenuationEffectParameter = "Attenuation";

		public const string SendTargetProperty = "sendTarget";
		public const string ColorIndexParameter = "userColorIndex";
		public const string WetMixProperty = "enableWetMix";

		public static AudioMixerGroup DuplicateBroAudioTrack(AudioMixer mixer, AudioMixerGroup mainTrack, AudioMixerGroup sourceTrack, string newTrackName)
		{
			// Using [DuplicateGroupRecurse] method on AudioMixerController will cause some unexpected result.
			// Create a new one and copy the setting manually might be better.

			AudioClassReflectionHelper reflection = new AudioClassReflectionHelper();

			AudioMixerGroup newGroup = ExecuteMethod("CreateNewGroup", new object[] { newTrackName, false }, reflection.MixerClass, mixer) as AudioMixerGroup;
			if (newGroup != null)
			{
				ExecuteMethod("AddChildToParent", new object[] { newGroup, mainTrack }, reflection.MixerClass, mixer);
				ExecuteMethod("AddGroupToCurrentView", new object[] { newGroup }, reflection.MixerClass, mixer);
				ExecuteMethod("OnSubAssetChanged", null, reflection.MixerClass, mixer);

				CopyColorIndex(sourceTrack, newGroup, reflection);
				CopyMixerGroupValue(ExposedParameterType.Volume, mixer, reflection.MixerGroupClass, sourceTrack.name, newGroup);
				CopyMixerGroupValue(ExposedParameterType.Pitch, mixer, reflection.MixerGroupClass, sourceTrack.name, newGroup);

				ExposeParameter(ExposedParameterType.Volume, newGroup, reflection);
				ExposeParameter(ExposedParameterType.Pitch, newGroup, reflection);

				object effect = CopySendEffect(sourceTrack, newGroup, reflection);
				ExposeParameter(ExposedParameterType.EffectSend, newGroup, reflection, effect);
			}
			return newGroup;
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

		private static void CopyMixerGroupValue(ExposedParameterType parameterType, AudioMixer mixer, Type mixerGroupClass, string sourceTrackName, AudioMixerGroup to)
		{
			MethodName setterMethod = default;
			string getterParaName = null;
			var snapshot = mixer.FindSnapshot(DefaultSnapshot);

			switch (parameterType)
			{
				case ExposedParameterType.Volume:
					setterMethod = MethodName.SetValueForVolume;
					getterParaName = sourceTrackName;
					break;
				case ExposedParameterType.Pitch:
					setterMethod = MethodName.SetValueForPitch;
					getterParaName = sourceTrackName + BroName.PitchParaNameSuffix;
					break;
				case ExposedParameterType.EffectSend:
					UnityEngine.Debug.LogError("This method can only be used with mixerGroup only");
					return;
			}

			// don't know why this can't be done, it always returns 0f
			//object value = ExecuteMethod(getterMethod.ToString(), new object[] { mixer, snapshot }, mixerGroupClass, from);

			if(mixer.GetFloat(getterParaName, out float value))
			{
				ExecuteMethod(setterMethod.ToString(), new object[] { mixer, snapshot, value }, mixerGroupClass, to);
			}
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

		public static void ExposeParameter(ExposedParameterType parameterType, AudioMixerGroup mixerGroup, AudioClassReflectionHelper reflection = null, params object[] additionalObjects)
		{
            reflection = reflection ?? new AudioClassReflectionHelper();
			AudioMixer audioMixer = mixerGroup.audioMixer;

			switch (parameterType)
			{
				case ExposedParameterType.Volume:
					if (TryGetGUID(MethodName.GetGUIDForVolume, reflection.MixerGroupClass, mixerGroup, out GUID volGUID))
					{
						object volParaPath = CreateParameterPathInstance("AudioGroupParameterPath", mixerGroup, volGUID);
						CustomParameterExposer.AddExposedParameter(mixerGroup.name, volParaPath, volGUID, audioMixer, reflection);
					}
					break;
				case ExposedParameterType.Pitch:
					if (TryGetGUID(MethodName.GetGUIDForPitch, reflection.MixerGroupClass, mixerGroup, out GUID pitchGUID))
					{
						object pitchParaPath = CreateParameterPathInstance("AudioGroupParameterPath", mixerGroup, pitchGUID);
						CustomParameterExposer.AddExposedParameter(mixerGroup.name + BroName.PitchParaNameSuffix, pitchParaPath, pitchGUID, audioMixer, reflection);
					}
					break;
				case ExposedParameterType.EffectSend:
					object effect = additionalObjects[0];
					if (TryGetGUID(MethodName.GetGUIDForMixLevel, reflection.EffectClass, effect, out GUID effectGUID))
					{
						object effectParaPath = CreateParameterPathInstance("AudioEffectParameterPath", mixerGroup, effect, effectGUID);
						CustomParameterExposer.AddExposedParameter(mixerGroup.name + BroName.SendParaNameSuffix, effectParaPath, effectGUID, audioMixer, reflection);
					}
					break;
			}
		}

		private static bool TryGetGUID(MethodName methodName,Type type,object target,out GUID guid)
		{
			guid = default;
			object obj = ExecuteMethod(methodName.ToString(), ReflectionExtension.Void, type, target);
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