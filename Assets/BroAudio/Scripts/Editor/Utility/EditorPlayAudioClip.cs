using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

#if UNITY_EDITOR
namespace Ami.Extension
{
	public static class EditorPlayAudioClip
	{
#if UNITY_2020_2_OR_NEWER
		public const string PlayClipMethodName = "PlayPreviewClip";
        public const string StopClipMethodName = "StopAllPreviewClips";
#else
		public const string PlayClipMethodName = "PlayClip";
        public const string StopClipMethodName = "StopAllClips";
#endif


        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
		{
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
                PlayClipMethodName,
				BindingFlags.Static | BindingFlags.Public,
				null,
				new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
				null
			);

			method?.Invoke(
				null,
				new object[] { clip, startSample, loop }
			);
		}

		public static void StopAllClips()
		{
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(
                StopClipMethodName,
				BindingFlags.Static | BindingFlags.Public,
				null,
				new Type[] { },
				null
			);

			method?.Invoke(
				null,
				new object[] { }
			);
		}
	} 
}
#endif