using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Threading;

#if UNITY_EDITOR
namespace Ami.Extension
{
    public class EditorPlayAudioClip
	{
		public delegate void StopAllPreviewClips();

		public const string PlayWithVolumeSetting = "[Experimental]\nRight-click to play at the current volume (maximum at 0dB)";
#if UNITY_2020_2_OR_NEWER
		public const string PlayClipMethodName = "PlayPreviewClip";
        public const string StopClipMethodName = "StopAllPreviewClips";
#else
		public const string PlayClipMethodName = "PlayClip";
        public const string StopClipMethodName = "StopAllClips";
#endif

		private static EditorPlayAudioClip _instance = null;
		public static EditorPlayAudioClip Instance
		{
			get
			{
				_instance = _instance ?? new EditorPlayAudioClip();
				return _instance;
            }
		}

        public PlaybackIndicatorUpdater PlaybackIndicator { get; private set; }

		public Action OnFinished;

		private StopAllPreviewClips _stopAllPreviewClipsDelegate = null;
		private CancellationTokenSource _currentPlayingTaskCanceller = null;
		private CancellationTokenSource _currentAudioSourceTaskCanceller = null;
		private AudioSource _currentEditorAudioSource = null;
		private bool _isReplaying = false;

        public EditorPlayAudioClip()
        {
            PlaybackIndicator = new PlaybackIndicatorUpdater();
			_stopAllPreviewClipsDelegate = GetStopAllPreviewClipsDelegate();
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void PlayClipByAudioSource(AudioClip audioClip, float volume, float startTime, float endTime, bool selfLoop = false, Action onReplay = null, float pitch = 1f)
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

        public void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
		{
			int startSample = AudioExtension.GetTimeSample(audioClip, startTime);
			int endSample = AudioExtension.GetTimeSample(audioClip, endTime);
			PlayClip(audioClip, startSample, endSample, loop);
		}

		public void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
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
					_stopAllPreviewClipsDelegate.Invoke();
					method.Invoke(null, parameters);
					AsyncTaskExtension.DelayInvoke(lengthInMs, Replay, _currentPlayingTaskCanceller.Token);
				}
			}
		}

		public void StopAllClips()
		{
            _isReplaying = false;
            StopStaticPreviewClipsAndCancelTask();
			DestroyPreviewAudioSourceAndCancelTask();
		}

		private void DestroyPreviewAudioSourceAndCancelTask()
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

		private void StopStaticPreviewClipsAndCancelTask()
        {
            CancelTask(ref _currentPlayingTaskCanceller);
            _stopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();
            TriggerOnFinished();
        }

        private void TriggerOnFinished()
        {
			if(!_isReplaying)
			{
                OnFinished?.Invoke();
                OnFinished = null;
            }	
        }

        private StopAllPreviewClips GetStopAllPreviewClipsDelegate()
		{
			Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

			Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
			MethodInfo method = audioUtilClass.GetMethod(StopClipMethodName, BindingFlags.Static | BindingFlags.Public, null, new Type[] { }, null);
			return Delegate.CreateDelegate(typeof(StopAllPreviewClips), method) as StopAllPreviewClips;
		}

		public void AddPlaybackIndicatorListener(Action action)
		{
			RemovePlaybackIndicatorListener(action);	
			PlaybackIndicator.OnUpdate += action;
			PlaybackIndicator.OnEnd += action;
		}

		public void RemovePlaybackIndicatorListener(Action action)
		{
			PlaybackIndicator.OnUpdate -= action;
			PlaybackIndicator.OnEnd -= action;
		}

		private void CancelTask(ref CancellationTokenSource cancellation)
		{
			if (cancellation != null && cancellation.Token.CanBeCanceled)
			{
				cancellation.Cancel();
				cancellation?.Dispose();
				cancellation = null;
			}
		}

        private void Dispose()
        {
			OnFinished = null;
            StopStaticPreviewClipsAndCancelTask();
            DestroyPreviewAudioSourceAndCancelTask();
			PlaybackIndicator = null;
			_instance = null;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange mode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (mode == PlayModeStateChange.ExitingEditMode || mode == PlayModeStateChange.ExitingPlayMode)
            {
				Dispose();
            }
        }
    } 
}
#endif