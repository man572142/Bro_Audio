using System;
using System.Reflection;
using UnityEditor;

namespace Ami.Extension
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
				if (_mixerClass == null)
				{
					_mixerClass = GetUnityAudioEditorClass("AudioMixerController");
				}
				return _mixerClass;
			}
		}

		public Type MixerGroupClass
		{
			get
			{
				if (_mixerGroupClass == null)
				{
					_mixerGroupClass = GetUnityAudioEditorClass("AudioMixerGroupController");
				}
				return _mixerGroupClass;
			}
		}

		public Type EffectClass
		{
			get
			{
				if (_effectClass == null)
				{
					_effectClass = GetUnityAudioEditorClass("AudioMixerEffectController");
				}
				return _effectClass;
			}
		}

		public Type EffectParameterPath
		{
			get
			{
				if (_effectParameterPath == null)
				{
					_effectParameterPath = GetUnityAudioEditorClass("AudioEffectParameterPath");
				}
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