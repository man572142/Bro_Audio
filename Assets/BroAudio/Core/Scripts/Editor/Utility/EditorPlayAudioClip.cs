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

		public static Action OnFinished;

		private static CancellationTokenSource _currentPlayingTaskCanceller = null;
		private static CancellationTokenSource _currentAudioSourceTaskCanceller = null;
		private static AudioSource _currentEditorAudioSource = null;
		private static bool _isReplaying = false;

		public static void PlayClipByAudioSource(AudioClip audioClip, float volume, float startTime, float endTime, bool selfLoop = false, Action onReplay = null, float pitch = 1f)
		{
			StopStaticPreviewClipsAndCancelTask();
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _currentAudioSourceTaskCanceller);
				_currentEditorAudioSource.Stop();
			}
			else
			{
				var gameObj = new GameObject("PreviewAudioClip");
				gameObj.tag = "EditorOnly";
				gameObj.hideFlags = HideFlags.HideAndDontSave;
				_currentEditorAudioSource = gameObj.AddComponent<AudioSource>();
			}

			AudioSource audioSource = _currentEditorAudioSource;
			audioSource.clip = audioClip;
			audioSource.volume = volume;
			audioSource.playOnAwake = false;
			audioSource.time = startTime;
			audioSource.pitch = pitch;

			audioSource.Play();
			PlaybackIndicator.Start(selfLoop);

			float duration = audioClip.length - endTime - startTime;
			_currentAudioSourceTaskCanceller = _currentAudioSourceTaskCanceller ?? new CancellationTokenSource();

			_isReplaying = onReplay != null;
            if (!_isReplaying)
			{
				if(selfLoop)
				{
					onReplay = Replay;
				}
				else
				{
					onReplay = DestroyPreviewAudioSourceAndCancelTask;
                }
			}

            AsyncTaskExtension.DelayInvoke(duration, onReplay, _currentAudioSourceTaskCanceller.Token);

            void Replay()
			{
				if(audioSource != null && _currentAudioSourceTaskCanceller != null)
				{
					_currentEditorAudioSource.Stop();
					audioSource.time = startTime;
					audioSource.Play();
					AsyncTaskExtension.DelayInvoke(duration, Replay, _currentAudioSourceTaskCanceller.Token);
				}
			}
		}

        public static void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
		{
			int startSample = AudioExtension.GetTimeSample(audioClip, startTime);
			int endSample = AudioExtension.GetTimeSample(audioClip, endTime);
			PlayClip(audioClip, startSample, endSample, loop);
		}

		public static void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
		{
			StopAllClips();

			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(PlayClipMethodName,
				BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);

			var parameters = new object[] { audioClip, startSample, false };
			if (method != null)
			{
				method.Invoke(null, parameters);
				PlaybackIndicator.Start(loop);
			}

			int sampleLength = audioClip.samples - startSample - endSample;
			int lengthInMs = (int)Math.Round(sampleLength / (double)audioClip.frequency * TimeExtension.SecondInMilliseconds, MidpointRounding.AwayFromZero);
			_currentPlayingTaskCanceller = _currentPlayingTaskCanceller ?? new CancellationTokenSource();
			if (loop)
			{
				AsyncTaskExtension.DelayInvoke(lengthInMs, Replay, _currentPlayingTaskCanceller.Token);
			}
			else
			{
				AsyncTaskExtension.DelayInvoke(lengthInMs, StopStaticPreviewClipsAndCancelTask, _currentPlayingTaskCanceller.Token);
			}

			void Replay()
			{
				if(method != null && _currentPlayingTaskCanceller != null)
				{
					StopAllPreviewClips();
					method.Invoke(null, parameters);
					AsyncTaskExtension.DelayInvoke(lengthInMs, Replay, _currentPlayingTaskCanceller.Token);
				}
			}
		}

		public static void StopAllClips()
		{
            _isReplaying = false;
            StopStaticPreviewClipsAndCancelTask();
			DestroyPreviewAudioSourceAndCancelTask();
		}

		private static void DestroyPreviewAudioSourceAndCancelTask()
		{
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _currentAudioSourceTaskCanceller);

				_currentEditorAudioSource.Stop();
				UnityEngine.Object.DestroyImmediate(_currentEditorAudioSource.gameObject);
				PlaybackIndicator.End();
				_currentEditorAudioSource = null;
				TriggerOnFinished();
            }
		}

		private static void StopStaticPreviewClipsAndCancelTask()
        {
            CancelTask(ref _currentPlayingTaskCanceller);
            StopAllPreviewClips();
            PlaybackIndicator.End();
            TriggerOnFinished();
        }

        private static void TriggerOnFinished()
        {
			if(!_isReplaying)
			{
                OnFinished?.Invoke();
                OnFinished = null;
            }	
        }

        private static void StopAllPreviewClips()
		{
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(StopClipMethodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] { }, null);

			method?.Invoke(null, new object[] { });
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