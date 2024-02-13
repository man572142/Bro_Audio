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
		public const string SendEffectName = "Send";
		public const string AttenuationEffectParameter = "Attenuation";

		public const string SendTargetProperty = "sendTarget";
		public const string ColorIndexParameter = "userColorIndex";
		public const string WetMixProperty = "enableWetMix";

		public static AudioMixerGroup DuplicateBroAudioTrack(AudioMixer mixer, AudioMixerGroup parentTrack, AudioMixerGroup sourceTrack, string newTrackName, ExposedParameterType exposedParameterType = ExposedParameterType.All)
		{
			// Using [DuplicateGroupRecurse] method on AudioMixerController will cause some unexpected result.
			// Create a new one and copy the setting manually might be better.

			AudioClassReflectionHelper reflection = new AudioClassReflectionHelper();

			AudioMixerGroup newGroup = ExecuteMethod("CreateNewGroup", new object[] { newTrackName, false }, reflection.MixerClass, mixer) as AudioMixerGroup;
			if (newGroup != null)
			{
				ExecuteMethod("AddChildToParent", new object[] { newGroup, parentTrack }, reflection.MixerClass, mixer);
				ExecuteMethod("AddGroupToCurrentView", new object[] { newGroup }, reflection.MixerClass, mixer);
				ExecuteMethod("OnSubAssetChanged", null, reflection.MixerClass, mixer);

				CopyColorIndex(sourceTrack, newGroup, reflection);
				CopyMixerGroupValue(ExposedParameterType.Volume, mixer, reflection.MixerGroupClass, sourceTrack.name, newGroup);
				//CopyMixerGroupValue(ExposedParameterType.Pitch, mixer, reflection.MixerGroupClass, sourceTrack.name, newGroup);

				object effect = CopySendEffect(sourceTrack, newGroup, reflection);

				ExposeParameterIfContains(ExposedParameterType.Volume);
                //ExposeParameterIfContains(ExposedParameterType.Pitch);
                ExposeParameterIfContains(ExposedParameterType.EffectSend, effect);
			}
			return newGroup;

			void ExposeParameterIfContains(ExposedParameterType targetType,params object[] additionalObjects)
			{
                if (exposedParameterType.Contains(targetType))
                {
                    ExposeParameter(targetType, newGroup, reflection, additionalObjects);
                }
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

			if(mixer.SafeGetFloat(getterParaName, out float value))
			{
				ExecuteMethod(setterMethod.ToString(), new object[] { mixer, snapshot, value }, mixerGroupClass, to);
			}
		}

		private static object CopySendEffect(AudioMixerGroup sourceGroup, AudioMixerGroup targetGroup, AudioClassReflectionHelper reflection)
		{
			if (TryGetFirstEffect(sourceGroup, SendEffectName, reflection,out object sourceSendEffect, out int effectIndex))
			{
				var sendTarget = GetProperty<object>(SendTargetProperty, reflection.EffectClass, sourceSendEffect);
				var clonedEffect = ExecuteMethod("CopyEffect", new object[] { sourceSendEffect }, reflection.MixerClass, sourceGroup.audioMixer);
				SetProperty(SendTargetProperty, reflection.EffectClass, clonedEffect, sendTarget);
				SetProperty(WetMixProperty, reflection.EffectClass, clonedEffect, true);
				ExecuteMethod("InsertEffect", new object[] { clonedEffect, effectIndex }, reflection.MixerGroupClass, targetGroup);
				return clonedEffect;
			}
			return null;
		}

		private static bool TryGetFirstEffect(AudioMixerGroup mixerGroup,string targetEffectName ,AudioClassReflectionHelper reflection,out object result, out int effectIndex)
		{
			result = null;
			effectIndex = 0;
			object[] effects = GetProperty<object[]>("effects", reflection.MixerGroupClass, mixerGroup);

			for(int i = 0; i < effects.Length;i++)
			{
				string effectName = GetProperty<string>("effectName", reflection.EffectClass, effects[i]);
				if (effectName == targetEffectName)
				{
					result = effects[i];
					effectIndex = i;
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
					object effect;
                    if (additionalObjects != null && additionalObjects.Length > 0)
					{
                        effect = additionalObjects[0];
                    }
					else if (!TryGetFirstEffect(mixerGroup, SendEffectName, reflection, out effect, out _))
					{
						UnityEngine.Debug.LogError($"Can't expose [{SendEffectName}] on AudioMixerGroup:{mixerGroup.name}");
						return;
                    }

					if (TryGetGUID(MethodName.GetGUIDForMixLevel, reflection.EffectClass, effect, out GUID effectGUID))
					{
						object effectParaPath = CreateParameterPathInstance("AudioEffectParameterPath", mixerGroup, effect, effectGUID);
						CustomParameterExposer.AddExposedParameter(mixerGroup.name + BroName.EffectParaNameSuffix, effectParaPath, effectGUID, audioMixer, reflection);
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