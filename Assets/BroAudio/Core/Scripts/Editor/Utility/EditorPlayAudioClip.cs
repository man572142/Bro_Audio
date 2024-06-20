using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Threading;
using Ami.BroAudio.Editor;
using Ami.BroAudio.Data;
using UnityEngine.Audio;
using Ami.BroAudio.Tools;
using Ami.Extension.Reflection;

#if UNITY_EDITOR
namespace Ami.Extension
{
    public class EditorPlayAudioClip
	{
		public class Data
		{
			public AudioClip AudioClip;
			public float Volume;
			public float StartPosition;
			public float EndPosition;
			public float FadeIn;
			public float FadeOut;

            public Data(AudioClip audioClip, float volume,Transport transport)
            {
				AudioClip = audioClip;
				Volume = volume;
				StartPosition = transport.StartPosition; 
				EndPosition = transport.EndPosition;
				FadeIn = transport.FadeIn;
				FadeOut = transport.FadeOut;
            }

			public Data(BroAudioClip broAudioClip)
			{
				AudioClip = broAudioClip.AudioClip; 
				Volume = broAudioClip.Volume;
				StartPosition = broAudioClip.StartPosition;
				EndPosition = broAudioClip.EndPosition;
				FadeIn = broAudioClip.FadeIn;
				FadeOut = broAudioClip.FadeOut;
			}

            public float Duration => AudioClip.length - EndPosition - StartPosition;
        }

        public delegate void PlayPreviewClip(AudioClip audioClip, int startSample, bool loop);
        public delegate void StopAllPreviewClips();

		public const string AudioUtilClassName = "AudioUtil";
        public const string IgnoreSettingTooltip = "Right-click to play the audio clip directly";
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
		private PlayPreviewClip _playPreviewClipDelegate = null;
		private CancellationTokenSource _currentPlayingTaskCanceller = null;
		private CancellationTokenSource _currentAudioSourceTaskCanceller = null;
		private AudioSource _currentEditorAudioSource = null;
		private Data _currentPlayingClip = null;
		private bool _isReplaying = false;
		private AudioMixer _mixer;
		private EditorAudioPreviewer _volumeTransporter = null;

		public StopAllPreviewClips StopAllPreviewClipsDelegate
		{
			get
			{
                if(_stopAllPreviewClipsDelegate == null)
				{
                    Type audioUtilClass = AudioClassReflectionHelper.GetUnityEditorClass(AudioUtilClassName);
                    MethodInfo stopMethod = audioUtilClass.GetMethod(StopClipMethodName, BindingFlags.Static | BindingFlags.Public);
                    _stopAllPreviewClipsDelegate = Delegate.CreateDelegate(typeof(StopAllPreviewClips), stopMethod) as StopAllPreviewClips;
                }
				return _stopAllPreviewClipsDelegate;
            }
		}

        public PlayPreviewClip PlayPreviewClipDelegate
        {
            get
            {
                if (_playPreviewClipDelegate == null)
                {
                    Type audioUtilClass = AudioClassReflectionHelper.GetUnityEditorClass(AudioUtilClassName);
                    MethodInfo playMethod = audioUtilClass.GetMethod(PlayClipMethodName, BindingFlags.Static | BindingFlags.Public);
                    _playPreviewClipDelegate = Delegate.CreateDelegate(typeof(PlayPreviewClip), playMethod) as PlayPreviewClip;
                }
                return _playPreviewClipDelegate;
            }
        }

        public EditorPlayAudioClip()
        {
            _mixer = Resources.Load<AudioMixer>(BroName.EditorAudioMixerPath);
            PlaybackIndicator = new PlaybackIndicatorUpdater();
			_volumeTransporter = new EditorAudioPreviewer(_mixer);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void PlayClipByAudioSource(Data clip, bool selfLoop = false, Action onReplay = null, float pitch = 1f)
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

			_currentPlayingClip = clip;
            AudioSource audioSource = _currentEditorAudioSource;
			audioSource.clip = clip.AudioClip;
			audioSource.playOnAwake = false;
			audioSource.time = clip.StartPosition;
			audioSource.pitch = pitch;
            audioSource.outputAudioMixerGroup = GetEditorMasterTrack();

            _volumeTransporter.SetData(clip);

            audioSource.Play();
			PlaybackIndicator.Start(selfLoop);
			_volumeTransporter.Start();

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

            AsyncTaskExtension.DelayInvoke(clip.Duration, onReplay, _currentAudioSourceTaskCanceller.Token);

			AudioMixerGroup GetEditorMasterTrack()
			{
				var tracks = _mixer != null ? _mixer.FindMatchingGroups("Master") : null;
				if(tracks != null && tracks.Length > 0)
				{
					return tracks[0];
				}
				else
				{
					Debug.LogError("Can't find EditorBroAudioMixer's Master audioMixerGroup, the fading and extra volume is not applied to the preview");
                    return null;
                }
            }
		}

        private void Replay()
        {
            if (_currentEditorAudioSource != null && _currentAudioSourceTaskCanceller != null)
            {
				_volumeTransporter.End();
				PlaybackIndicator.End();

                _currentEditorAudioSource.Stop();
                _currentEditorAudioSource.time = _currentPlayingClip.StartPosition;
                _currentEditorAudioSource.Play();

                _volumeTransporter.Start();
                PlaybackIndicator.Start();
                AsyncTaskExtension.DelayInvoke(_currentPlayingClip.Duration, Replay, _currentAudioSourceTaskCanceller.Token);
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

            PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            PlaybackIndicator.Start(loop);

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
				if(_currentPlayingTaskCanceller != null)
				{
					StopAllPreviewClipsDelegate.Invoke();
                    PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
					PlaybackIndicator.End();
					PlaybackIndicator.Start();
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
                _volumeTransporter.End();
                _currentEditorAudioSource = null;
				TriggerOnFinished();
            }
		}

		private void StopStaticPreviewClipsAndCancelTask()
        {
            CancelTask(ref _currentPlayingTaskCanceller);
            StopAllPreviewClipsDelegate.Invoke();
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
			_currentPlayingClip = null;
			_mixer = null;
			_volumeTransporter.Dispose();
            _volumeTransporter = null;
            StopStaticPreviewClipsAndCancelTask();
            DestroyPreviewAudioSourceAndCancelTask();
			PlaybackIndicator.Dispose();
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