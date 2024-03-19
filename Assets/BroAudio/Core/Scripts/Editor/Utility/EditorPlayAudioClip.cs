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
		public const string PlayWithVolumeSetting = "[Experimental]\nRight-click to play at the current volume (maximum at 0dB)";
#if UNITY_2020_2_OR_NEWER
		public const string PlayClipMethodName = "PlayPreviewClip";
        public const string StopClipMethodName = "StopAllPreviewClips";
#else
		public const string PlayClipMethodName = "PlayClip";
        public const string StopClipMethodName = "StopAllClips";
#endif
		public readonly static PlaybackIndicatorUpdater PlaybackIndicator = new PlaybackIndicatorUpdater();
		public static AudioClip CurrentPlayingClip { get; private set; }

		private static CancellationTokenSource _currentPlayingTaskCanceller = null;
		private static CancellationTokenSource _currentDestroyAudioSourceTaskCanceller = null;
		private static AudioSource _currentEditorAudioSource = null;

		public static void PlayClipByAudioSource(AudioClip audioClip, float volume, float startTime, float endTime, bool loop = false)
		{
			StopAllClipsWithoutDestoryAudioSource();
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _currentDestroyAudioSourceTaskCanceller);
				_currentEditorAudioSource.Stop();
			}
			else
			{
				var gameObj = new GameObject("PreviewAudioClip");
				gameObj.tag = "EditorOnly";
				gameObj.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
				_currentEditorAudioSource = gameObj.AddComponent<AudioSource>();
			}


			AudioSource audioSource = _currentEditorAudioSource;
			audioSource.clip = audioClip;
			audioSource.volume = volume;
			audioSource.playOnAwake = false;
			audioSource.loop = loop;
			audioSource.time = startTime;

			audioSource.Play();
			PlaybackIndicator.Start();
			CurrentPlayingClip = audioClip;

			float duration = audioClip.length - endTime - startTime;
			_currentDestroyAudioSourceTaskCanceller = _currentDestroyAudioSourceTaskCanceller ?? new CancellationTokenSource();
			AsyncTaskExtension.DelayInvoke(duration, DestroyPreviewAudioSource, _currentDestroyAudioSourceTaskCanceller.Token);
		}

		public static void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
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
				CurrentPlayingClip = audioClip;
			}

            float duration = audioClip.length - startTime - endTime;
            _currentPlayingTaskCanceller = _currentPlayingTaskCanceller ?? new CancellationTokenSource();
			AsyncTaskExtension.DelayInvoke(duration, StopAllClips, _currentPlayingTaskCanceller.Token);
		}

		public static void StopAllClips()
		{
			StopAllClipsWithoutDestoryAudioSource();
			DestroyPreviewAudioSource();
		}

		private static void DestroyPreviewAudioSource()
		{
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _currentDestroyAudioSourceTaskCanceller);

				_currentEditorAudioSource.Stop();
				UnityEngine.Object.DestroyImmediate(_currentEditorAudioSource.gameObject);
				_currentEditorAudioSource = null;
				CurrentPlayingClip = null;
			}
		}

		private static void StopAllClipsWithoutDestoryAudioSource()
		{
			CancelTask(ref _currentPlayingTaskCanceller);

			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(StopClipMethodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] { }, null);

			if (method != null)
			{
				method.Invoke(null, new object[] { });
				PlaybackIndicator.End();
				CurrentPlayingClip = null;
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

		private static void CancelTask(ref CancellationTokenSource cancellation)
		{
			if (cancellation != null && cancellation.Token.CanBeCanceled)
			{
				cancellation.Cancel();
				cancellation?.Dispose();
				cancellation = null;
			}
		}
	} 
}
#endif