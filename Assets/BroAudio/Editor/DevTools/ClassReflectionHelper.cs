using System;
using System.Reflection;
using UnityEditor;

namespace Ami.Extension.Reflection
{
    public class ClassReflectionHelper
    {
        public const string AudioUtilClassName = "AudioUtil";
        public const string MixerClassName = "AudioMixerController";
        public const string MixerGroupClassName = "AudioMixerGroupController";
        public const string MixerEffectClassName = "AudioMixerEffectController";
        public const string MixerEffectParameterPathClassName = "AudioEffectParameterPath";

        private Type _mixerClass = null;
        private Type _mixerGroupClass = null;
        private Type _effectClass = null;
        private Type _effectParameterPath = null;

        public Type MixerClass
        {
            get
            {
                _mixerClass ??= GetUnityAudioEditorClass(MixerClassName);
                return _mixerClass;
            }
        }

        public Type MixerGroupClass
        {
            get
            {
                _mixerGroupClass ??= GetUnityAudioEditorClass(MixerGroupClassName);
                return _mixerGroupClass;
            }
        }

        public Type EffectClass
        {
            get
            {
                _effectClass ??= GetUnityAudioEditorClass(MixerEffectClassName);
                return _effectClass;
            }
        }

        public Type EffectParameterPath
        {
            get
            {
                _effectParameterPath ??= GetUnityAudioEditorClass(MixerEffectParameterPathClassName);
                return _effectParameterPath;
            }
        }

        public static Type GetUnityAudioEditorClass(string className)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            return unityEditorAssembly?.GetType($"UnityEditor.Audio.{className}");
        }

        public static Type GetUnityEditorClass(string className)
        {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
            return unityEditorAssembly?.GetType($"UnityEditor.{className}");
        }

        public static T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
        {
            Type audioUtilClass = GetUnityEditorClass(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            return Delegate.CreateDelegate(typeof(T), method) as T;
        }
    }
}