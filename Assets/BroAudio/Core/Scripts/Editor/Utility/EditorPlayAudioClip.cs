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
using static Ami.Extension.AsyncTaskExtension;

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
		public enum MuteState { None, On, Off }

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
        // This is used to avoid the popup sound at the beginning of the fade-in due to the delay of mixer volume change
        public const float SetVolumeOffsetTime = 0.05f; 

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
		private CancellationTokenSource _playClipTaskCanceller = null;
		private CancellationTokenSource _audioSourceTaskCanceller = null;
		private AudioSource _currentEditorAudioSource = null;
		private Data _currentPlayingClip = null;
		private bool _isRecursionOutside = false;
		private AudioMixer _mixer = null;
		private EditorAudioPreviewer _volumeTransporter = null;
		private MuteState _previousMuteState = MuteState.None;

		public StopAllPreviewClips StopAllPreviewClipsDelegate
		{
			get
			{
                _stopAllPreviewClipsDelegate = _stopAllPreviewClipsDelegate ?? GetAudioUtilMethodDelegate<StopAllPreviewClips>(StopClipMethodName);
				return _stopAllPreviewClipsDelegate;
            }
		}

        public PlayPreviewClip PlayPreviewClipDelegate
        {
            get
            {
				_playPreviewClipDelegate = _playPreviewClipDelegate ?? GetAudioUtilMethodDelegate<PlayPreviewClip>(PlayClipMethodName);
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

        public async void PlayClipByAudioSource(Data clip, bool selfLoop = false, Action onReplay = null, float pitch = 1f)
		{
			StopStaticPreviewClipsAndCancelTask();
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _audioSourceTaskCanceller);
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
			audioSource.reverbZoneMix = 0f;
            _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;

            _volumeTransporter.SetData(clip);

            audioSource.Play();
			PlaybackIndicator.Start(selfLoop);
			_volumeTransporter.Start();
			EditorUtility.audioMasterMute = false;

            _audioSourceTaskCanceller = _audioSourceTaskCanceller ?? new CancellationTokenSource();

			float waitTime = clip.Duration;
            if (clip.FadeIn >= SetVolumeOffsetTime)
            {
				audioSource.volume = 0f;
				await DelaySeconds(SetVolumeOffsetTime, _audioSourceTaskCanceller.Token);
                audioSource.volume = 1f;
                waitTime -= SetVolumeOffsetTime;

                if (_audioSourceTaskCanceller.IsCanceled())
                {
                    return;
                }
            }
			
            await DelaySeconds(waitTime, _audioSourceTaskCanceller.Token);

            if (_audioSourceTaskCanceller.IsCanceled())
            {
                return;
            }

            _isRecursionOutside = onReplay != null;
            if (_isRecursionOutside)
			{
				onReplay.Invoke();
			}
			else
			{
                if (selfLoop)
                {
                    AudioSourceReplay();
                }
                else
                {
                    DestroyPreviewAudioSourceAndCancelTask();
                }
            }
		}

        private async void AudioSourceReplay()
        {
            if (_currentEditorAudioSource != null && _audioSourceTaskCanceller != null)
            {
				_volumeTransporter.End();
				PlaybackIndicator.End();

                _currentEditorAudioSource.Stop();
                _currentEditorAudioSource.time = _currentPlayingClip.StartPosition;
                _currentEditorAudioSource.Play();

                _volumeTransporter.Start();
                PlaybackIndicator.Start();
                await DelaySeconds(_currentPlayingClip.Duration, _audioSourceTaskCanceller.Token);
                if(!_audioSourceTaskCanceller.IsCanceled())
                {
                    // Recursive
                    AudioSourceReplay();
                }
            }
        }

        public void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
		{
			int startSample = AudioExtension.GetTimeSample(audioClip, startTime);
			int endSample = AudioExtension.GetTimeSample(audioClip, endTime);
			PlayClip(audioClip, startSample, endSample, loop);
		}

		public async void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
		{
			StopAllClips();

            PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            PlaybackIndicator.Start(loop);

            int sampleLength = audioClip.samples - startSample - endSample;
			int lengthInMs = (int)Math.Round(sampleLength / (double)audioClip.frequency * TimeExtension.SecondInMilliseconds, MidpointRounding.AwayFromZero);
			_playClipTaskCanceller = _playClipTaskCanceller ?? new CancellationTokenSource();
            await Delay(lengthInMs, _playClipTaskCanceller.Token);

            if(_playClipTaskCanceller.IsCanceled())
            {
                return;
            }

            if (loop)
			{
                AudioClipReplay(audioClip, startSample, loop, lengthInMs);
			}
			else
			{
				StopStaticPreviewClipsAndCancelTask();
            }
		}

        private async void AudioClipReplay(AudioClip audioClip, int startSample, bool loop, int lengthInMs)
        {
            if (_playClipTaskCanceller != null)
            {
                StopAllPreviewClipsDelegate.Invoke();
                PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
                PlaybackIndicator.End();
                PlaybackIndicator.Start();

                await Delay(lengthInMs, _playClipTaskCanceller.Token);
                if(!_playClipTaskCanceller.IsCanceled())
                {
                    // Recursive
                    AudioClipReplay(audioClip, startSample, loop, lengthInMs);
                }
            }
        }

        public void StopAllClips()
        {
            _isRecursionOutside = false;
            StopStaticPreviewClipsAndCancelTask();
            DestroyPreviewAudioSourceAndCancelTask();

            if (_previousMuteState != MuteState.None)
            {
                EditorUtility.audioMasterMute = _previousMuteState == MuteState.On;
                _previousMuteState = MuteState.None;
            }
        }

        private void DestroyPreviewAudioSourceAndCancelTask()
		{
			if (_currentEditorAudioSource)
			{
				CancelTask(ref _audioSourceTaskCanceller);

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
            CancelTask(ref _playClipTaskCanceller);
            StopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();
            TriggerOnFinished();
        }

        private void TriggerOnFinished()
        {
			if(!_isRecursionOutside)
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

        private T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
        {
            Type audioUtilClass = AudioClassReflectionHelper.GetUnityEditorClass(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            return Delegate.CreateDelegate(typeof(T), method) as T;
        }

        private AudioMixerGroup GetEditorMasterTrack()
        {
            var tracks = _mixer != null ? _mixer.FindMatchingGroups("Master") : null;
            if (tracks != null && tracks.Length > 0)
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
}
#endif