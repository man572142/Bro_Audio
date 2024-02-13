using System;
using System.Reflection;
using UnityEditor;

namespace Ami.Extension.Reflection
{
	public class AudioClassReflectionHelper
	{
		private Type _mixerClass = null;
		private Type _mixerGroupClass = null;
		private Type _effectClass = null;
		private Type _effectParameterPath = null;

		public Type MixerClass
		{
			get
			{
                _mixerClass = _mixerClass ?? GetUnityAudioEditorClass("AudioMixerController");
				return _mixerClass;
			}
		}

		public Type MixerGroupClass
		{
			get
			{
                _mixerGroupClass = _mixerGroupClass ?? GetUnityAudioEditorClass("AudioMixerGroupController");
                return _mixerGroupClass;
			}
		}

		public Type EffectClass
		{
			get
			{
                _effectClass = _effectClass ?? GetUnityAudioEditorClass("AudioMixerEffectController");
                return _effectClass;
			}
		}

		public Type EffectParameterPath
		{
			get
			{
                _effectParameterPath = _effectParameterPath ?? GetUnityAudioEditorClass("AudioEffectParameterPath");
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
	}
}