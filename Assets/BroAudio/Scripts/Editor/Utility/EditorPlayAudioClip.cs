using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Threading;

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
		public readonly static PlaybackIndicatorUpdater PlaybackIndicator = new PlaybackIndicatorUpdater();

		private static CancellationTokenSource CurrentPlayingTaskCanceller = null;

		public static void PlayClip(AudioClip audioClip, float startTime = 0f, float endTime = 0f, bool loop = false)
		{
			StopAllClips();

			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(PlayClipMethodName,
				BindingFlags.Static | BindingFlags.Public,null,	new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },null);


			int startSample = Mathf.RoundToInt(audioClip.frequency * startTime);
			if (method != null)
			{
				method.Invoke(null,	new object[] { audioClip, startSample, loop });
				PlaybackIndicator.Start();
			}

            float duration = audioClip.length - startTime - endTime;
            CurrentPlayingTaskCanceller = CurrentPlayingTaskCanceller ?? new CancellationTokenSource();
			AsyncTaskExtension.DelayInvoke(duration, StopAllClips, CurrentPlayingTaskCanceller.Token);
		}

		public static void StopAllClips()
		{
			if (CurrentPlayingTaskCanceller != null && CurrentPlayingTaskCanceller.Token.CanBeCanceled)
			{
				CurrentPlayingTaskCanceller.Cancel();
				CurrentPlayingTaskCanceller?.Dispose();
				CurrentPlayingTaskCanceller = null;
			}

			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(StopClipMethodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] { }, null);

			if(method != null)
			{
				method.Invoke(null,new object[] { });
				PlaybackIndicator.End();
			}
		}

		public static void AddPlaybackIndicatorListener(Action action)
		{
			RemovePlaybackIndicatorListener(action);	
			PlaybackIndicator.OnUpdate += action;
			PlaybackIndicator.OnEnd += action;
		}

		public static void RemovePlaybackIndicatorListener(Action action)
		{
			PlaybackIndicator.OnUpdate -= action;
			PlaybackIndicator.OnEnd -= action;
		}
	} 
}
#endif