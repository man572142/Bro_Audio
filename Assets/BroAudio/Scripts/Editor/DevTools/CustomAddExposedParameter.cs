using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEngine.Audio;
using static Ami.Extension.ReflectionExtension;
using System.Linq;

namespace Ami.Extension
{
	public static class CustomAddExposedParameter
	{
		public const string ExposedParametersPropName = "exposedParameters";
		public const string ChachedExposedParametersPropName = "exposedParamCache";

		private struct ExposedAudioParameter
		{
			public GUID Guid;
			public string Name;
		}

		// 1. 取得Mixer上所有Parameter
		// 2. 加入新的Parameter
		// 3. 不確定是不是要Sort
		// 4. assign回去Mixer
		// 5. 執行OnChangedExposedParameter
		// 6. 取得exposedParamCache並assign
		// 7. 執行RepaintAudioMixerAndInspectors

		public static void AddExposedParameter(string exposedName,object effect,AudioMixerGroup mixerGroup,AudioClassReflectionHelper reflection)
		{
			if (!TryGetGUIDForMixLevel(reflection.EffectClass, effect, out GUID guid) || guid == default)
			{
				Debug.LogError("Trying to expose parameter with default GUID.");
				return;
			}

			object paraPathInstance = CreateAudioEffectParameterPathInstance(reflection.EffectParameterPath, mixerGroup, effect, guid);
			if (paraPathInstance == null)
			{
				Debug.LogError("Trying to expose null parameter.");
				return;
			}

			ExposedAudioParameter[] exposedParameters = GetProperty<ExposedAudioParameter[]>(ExposedParametersPropName, reflection.MixerClass, mixerGroup.audioMixer);
			Debug.Log($"exposedParameters?{exposedParameters != null}");

			if (ContainsExposedParameter(exposedParameters, guid))
			{
				Debug.LogError("Cannot expose the same parameter more than once.");
				return;
			}

			var parameters = new List<ExposedAudioParameter>(exposedParameters);
			var newParam = new ExposedAudioParameter();
			newParam.Name = exposedName;
			newParam.Guid = guid;
			parameters.Add(newParam);

			// TODO: sort it in bro's way
			//parameters.Sort(SortFuncForExposedParameters);

			exposedParameters = parameters.ToArray();
			SetProperty(ExposedParametersPropName, reflection.MixerClass, mixerGroup.audioMixer, exposedParameters);

			ExecuteMethod("OnChangedExposedParameter", new object[] { }, reflection.MixerClass, mixerGroup.audioMixer);

			var exposedParamCache = GetProperty<Dictionary<GUID, object>>(ChachedExposedParametersPropName, reflection.MixerClass, mixerGroup.audioMixer);
			exposedParamCache[guid] = paraPathInstance;
			SetProperty(ChachedExposedParametersPropName, reflection.MixerClass, mixerGroup.audioMixer, exposedParamCache);

			//AudioMixerUtility.RepaintAudioMixerAndInspectors();
			Type mixerUtil = AudioClassReflectionHelper.GetUnityAudioEditorClass("AudioMixerUtility");
			ExecuteMethod("RepaintAudioMixerAndInspectors", new object[] { }, mixerUtil, null);
		}

		private static bool ContainsExposedParameter(ExposedAudioParameter[] exposedParameters, GUID parameter)
		{
			return exposedParameters.Where(val => val.Guid == parameter).ToArray().Length > 0;
		}

		private static bool TryGetGUIDForMixLevel(Type effectClass, object effect, out GUID guid)
		{
			guid = default;
			MethodInfo method = effectClass?.GetMethod(
						"GetGUIDForMixLevel",
						new Type[] { }
					);

			object obj = method.Invoke(
				effect,
				new object[] { }
			);
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
				Debug.LogError($"{BroAudioReflection.LogTitle} Cast GUID failed! object :{obj}");
			}
			return guid != default && !guid.Empty();
		}

		private static object CreateAudioEffectParameterPathInstance(Type effectParaPathClass, AudioMixerGroup mixerGroup, object effect, GUID guid)
		{
			var constructors = effectParaPathClass.GetConstructors();
			if (constructors != null && constructors.Length > 0)
			{
				object[] parameters = new object[] { mixerGroup, effect, guid };
				object instance = constructors[0].Invoke(parameters);
				return instance;
			}

			return null;
		}

	}

}