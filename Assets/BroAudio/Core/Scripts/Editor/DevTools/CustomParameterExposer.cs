using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityEngine.Audio;
using static Ami.Extension.ReflectionExtension;
using System.Linq;

namespace Ami.Extension.Reflection
{
	public static class CustomParameterExposer 
	{
		public const string ExposedParametersPropName = "exposedParameters";
		public const string CachedExposedParametersGetterName = "exposedParamCache";
		public const string CachedExposedParametersFieldName = "m_ExposedParamPathCache";

		private class ReflectedExposedAudioParameter : ReflectedClass
		{
			private const string GUIDFieldName = "guid";
			private const string NameFieldName = "name";

			public ReflectedExposedAudioParameter(Type targetType,GUID guid, string name) : base(targetType)
			{
				SetInstanceField(GUIDFieldName, guid);
				GUID = guid;

				SetInstanceField(NameFieldName, name);
				Name = name;
			}

			public ReflectedExposedAudioParameter(object instance) : base(instance)
			{
				GUID = GetInstanceField<GUID>(GUIDFieldName);
				Name = GetInstanceField<string>(NameFieldName);
			}

			public GUID GUID { get; private set; }
			public string Name { get; private set; }
		}

		public static void AddExposedParameter(string exposedName,object paraPathInstance,GUID guid ,AudioMixer audioMixer,ClassReflectionHelper reflection)
		{
			if (guid == default)
			{
				Debug.LogError("Trying to expose parameter with default GUID.");
				return;
			}

			if (paraPathInstance == null)
			{
				Debug.LogError("Trying to expose null parameter.");
				return;
			}

			object exposedParameters = GetProperty<object>(ExposedParametersPropName, reflection.MixerClass, audioMixer);
			if(!TryCastObjectArrayToList(exposedParameters,out List<ReflectedExposedAudioParameter> exposedParameterList))
			{
				Debug.LogError("Cast current exposed parameters failed");
			}

			if (ContainsExposedParameter(exposedParameterList, guid))
			{
				Debug.LogError("Cannot expose the same parameter more than once.");
				return;
			}

			Type parameterType = ClassReflectionHelper.GetUnityAudioEditorClass("ExposedAudioParameter");
			var newParam = new ReflectedExposedAudioParameter(parameterType, guid, exposedName);
			exposedParameterList.Add(newParam);

			AddElementTo(ref exposedParameters, newParam.Instance, parameterType);
			SetProperty(ExposedParametersPropName, reflection.MixerClass, audioMixer, exposedParameters);
			ExecuteMethod("OnChangedExposedParameter", ReflectionExtension.Void, reflection.MixerClass, audioMixer);

			var exposedParamCache = GetProperty<IDictionary>(CachedExposedParametersGetterName, reflection.MixerClass, audioMixer,PrivateFlag);
			exposedParamCache[guid] = paraPathInstance;
			SetField(CachedExposedParametersFieldName, reflection.MixerClass, audioMixer, exposedParamCache, PrivateFlag);

			//AudioMixerUtility.RepaintAudioMixerAndInspectors();
			Type mixerUtil = ClassReflectionHelper.GetUnityEditorClass("AudioMixerUtility");
			ExecuteMethod("RepaintAudioMixerAndInspectors", ReflectionExtension.Void, mixerUtil, null,BindingFlags.Public | BindingFlags.Static);
		}

		private static bool ContainsExposedParameter(IEnumerable<ReflectedExposedAudioParameter> parameters, GUID parameter)
		{
			return parameters.Where(val => val.GUID == parameter).ToArray().Length > 0;
		}

		private static bool TryCastObjectArrayToList(object arrayObject,out List<ReflectedExposedAudioParameter> resultList)
		{
			resultList = null;
			Type type = arrayObject.GetType();
			if(type.IsArray)
			{
				Array array = (Array)arrayObject;
				object[] resultArray = new object[array.Length];
				array.CopyTo(resultArray, 0);

				resultList = new List<ReflectedExposedAudioParameter>();
				foreach(var obj in resultArray)
				{
					resultList.Add(new ReflectedExposedAudioParameter(obj));
				}

				return resultList.Count != 0;
			}

			return false;
		}

		private static void AddElementTo(ref object originalArray,object value,Type elementType)
		{
			Type type = originalArray.GetType();
			if(type.IsArray)
			{
				Array array = (Array)originalArray;
				var newArray = Array.CreateInstance(elementType, array.Length + 1);
				array.CopyTo(newArray, 0);
				newArray.SetValue(value, newArray.Length - 1);
				originalArray = newArray;
			}
		}
	}

}