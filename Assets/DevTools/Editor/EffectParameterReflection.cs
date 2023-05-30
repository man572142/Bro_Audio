using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

public class EffectParameterReflection
{
	public class UnityAudioClassReflection : IDisposable
	{
        public readonly Type MixerClass = GetUnityAudioEditorClass("AudioMixerController");
        public readonly Type MixerGroupClass = GetUnityAudioEditorClass("AudioMixerGroupController");
        public readonly Type EffectClass = GetUnityAudioEditorClass("AudioMixerEffectController");
        public readonly Type EffectParameterPath = GetUnityAudioEditorClass("AudioEffectParameterPath");

		public void Dispose()
		{
        }
	}

	//[MenuItem("BroAudio/[Dev] Expose All Send Mix Level")]
    public static void ExposeSendParameter()
    {
        if(!TryLoadMixerFromResources("BroAudioMixer", out var mixer)) return;
       
        using (UnityAudioClassReflection reflect = new UnityAudioClassReflection())
        {
            var groups = mixer.FindMatchingGroups("Track");

            for (int i = 0; i < groups.Length; i++)
            {
                AudioMixerGroup mixerGroup = groups[i];

                object[] effects = GetProperty<object[]>(reflect.MixerGroupClass, mixerGroup, "effects");

                foreach (var effect in effects)
                {
                    string effectName = GetProperty<string>(reflect.EffectClass, effect, "effectName");
                    if (effectName == "Send" && TryGetGUIDForMixLevel(reflect.EffectClass, effect, out GUID guid))
                    {
                        object parameterPath = CreateAudioEffectParameterPathInstance(reflect.EffectParameterPath, mixerGroup, effect, guid);
                        ExposeParameter(reflect.MixerClass, mixer, parameterPath);
                        // TODO : Exclude parameter that already exist.
                    }
                }
            }
        }
    }

    //[MenuItem("BroAudio/[Dev] Enable All Send Wet Mix")]
    public static void EnableSendWetMix()
	{
        if (!TryLoadMixerFromResources("BroAudioMixer", out var mixer)) return;

        using (UnityAudioClassReflection reflect = new UnityAudioClassReflection())
        {
            var groups = mixer.FindMatchingGroups("Track");

            for (int i = 0; i < groups.Length; i++)
            {
                AudioMixerGroup mixerGroup = groups[i];

                object[] effects = GetProperty<object[]>(reflect.MixerGroupClass, mixerGroup, "effects");

                foreach (var effect in effects)
                {
                    string effectName = GetProperty<string>(reflect.EffectClass, effect, "effectName");
                    if (effectName == "Send")
                    {
                        PropertyInfo property = reflect.EffectClass?.GetProperty("enableWetMix");
                        property.SetValue(effect,true);
                    }
                }
            }
        }
    }

    //[MenuItem("BroAudio/[Dev] Set All Send Wet Mix Level")]
    public static void SetSendWetMixLevel()
    {
        if (!TryLoadMixerFromResources("BroAudioMixer", out var mixer)) return;

        using (UnityAudioClassReflection reflect = new UnityAudioClassReflection())
        {
            var groups = mixer.FindMatchingGroups("Track");

            for (int i = 0; i < groups.Length; i++)
            {
                AudioMixerGroup mixerGroup = groups[i];

                object[] effects = GetProperty<object[]>(reflect.MixerGroupClass, mixerGroup, "effects");

                foreach (var effect in effects)
                {
                    string effectName = GetProperty<string>(reflect.EffectClass, effect, "effectName");
                    if (effectName == "Send")
                    {
                        var snapshot = mixer.FindSnapshot("Snapshot");
                        SetValueForMixLevel(reflect.EffectClass, effect, mixer, snapshot, -80f);
                    }
                }
            }
        }
    }

    public static bool TryLoadMixerFromResources(string mixerName, out AudioMixer mixer)
    {
        mixer = Resources.Load(mixerName) as AudioMixer;
        if (mixer == null)
        {
            Debug.LogError($"Can't get mixer:{mixerName} from Resources folder ,");
            return false;
        }
        return true;
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

    private static void ExposeParameter(Type mixerClass, AudioMixer mixer, object parameter)
    {
        MethodInfo method = mixerClass?.GetMethod(
                    "AddExposedParameter"
                );

        object obj = method.Invoke(
            mixer,
            new object[] { parameter }
        );
    }

    private static bool TryGetGUIDForMixLevel(Type effectClass, object effect,out GUID guid)
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

    private static void SetValueForMixLevel(Type effectClass, object effect,AudioMixer mixer,AudioMixerSnapshot snapshot, float value)
    {
        MethodInfo method = effectClass?.GetMethod("SetValueForMixLevel");

        method.Invoke(
            effect,
            new object[] {mixer,snapshot,value }
        );
    }

    private static bool TryGetGUIDForParameter(Type effectClass, object effect,string parameterName,out GUID guid)
	{
		guid = default;
		MethodInfo method = effectClass?.GetMethod(
					"GetGUIDForParameter",
					new Type[] { typeof(string) }
				);

		object obj = method.Invoke(
			effect,
			new object[] { parameterName }
		);
		return TryConvertGUID(obj,ref guid);
	}

	private static bool TryConvertGUID(object obj,ref GUID guid)
	{
        try
		{
			guid = (GUID)obj;
		}
		catch (InvalidCastException)
		{
			Debug.LogError($"[Reflection] Cast GUID failed! object :{obj}");
		}
        return guid != default && !guid.Empty();
    }

    private static Type GetUnityAudioEditorClass(string className)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        return unityEditorAssembly?.GetType($"UnityEditor.Audio.{className}");
    }

    private static T GetProperty<T>(Type type, object obj, string propertyName)
    {
        PropertyInfo property = type?.GetProperty(propertyName);
        try
        {
            return (T)property.GetValue(obj);
        }
        catch (InvalidCastException)
        {
            Debug.LogError($"[Reflection] Cast property failed. Property name:{propertyName}");
        }
        catch (NullReferenceException)
        {
            Debug.LogError($"[Reflection] Can't find property in {type.Name} with property name:{propertyName}");
        }
        return default(T);
    }
}