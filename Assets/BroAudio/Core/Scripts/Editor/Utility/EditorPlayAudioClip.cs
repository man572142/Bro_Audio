using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Ami.BroAudio.Editor;
using Ami.BroAudio.Data;
using UnityEngine.Audio;
using System.Threading.Tasks;
using System.Reflection;
using static Ami.BroAudio.Utility;
using static Ami.Extension.Reflection.ClassReflectionHelper;
using static Ami.Extension.TimeExtension;

#if UNITY_EDITOR
namespace Ami.Extension
{
    public class EditorPlayAudioClip
    {
        private enum MuteState { None, On, Off }

        public delegate void PlayPreviewClip(AudioClip audioClip, int startSample, bool loop);
        public delegate void StopAllPreviewClips();

        public const string IgnoreSettingTooltip = "Right-click to play the audio clip directly";
        private const string MixerSuspendFieldName = "m_EnableSuspend";
        private const string PlayClipMethodName = "PlayPreviewClip";
        private const string StopClipMethodName = "StopAllPreviewClips";

        private static EditorPlayAudioClip _instance = null;
        public static EditorPlayAudioClip Instance
        {
            get
            {
                _instance ??= new EditorPlayAudioClip();
                return _instance;
            }
        }

        public PlaybackIndicatorUpdater PlaybackIndicator { get; private set; }

        public Action OnFinished;
        private readonly StopAllPreviewClips _stopAllPreviewClipsDelegate;
        private readonly PlayPreviewClip _playPreviewClipDelegate;

        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private AudioSource _currentEditorAudioSource;
        private PreviewRequest _currentPreviewRequest;
        private AudioMixer _mixer;
        private AudioMixerGroup _masterTrack;
        private EditorVolumeTransporter _volumeTransporter;
        private MuteState _previousMuteState = MuteState.None;

        private CancellationTokenSource CancellationSource => _cancellationSource ??= new CancellationTokenSource();

        public EditorPlayAudioClip()
        {
            _mixer = Resources.Load<AudioMixer>(BroEditorUtility.EditorAudioMixerPath);
            PlaybackIndicator = new PlaybackIndicatorUpdater();
            _volumeTransporter = new EditorVolumeTransporter(_mixer);

            _stopAllPreviewClipsDelegate = GetAudioUtilMethodDelegate<StopAllPreviewClips>(StopClipMethodName);
            _playPreviewClipDelegate = GetAudioUtilMethodDelegate<PlayPreviewClip>(PlayClipMethodName);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        #region AudioSource
        public async void PlayClipByAudioSource(PreviewRequest req, bool selfLoop = false, ReplayData replayData = null)
        {
            try
            {
                await PlayClipByAudioSourceAsync(req, selfLoop, replayData);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipByAudioSourceAsync(PreviewRequest req, bool selfLoop, ReplayData replayData)
        {
            if(req.AudioClip == null)
            {
                return;
            }
            StopStaticPreviewClipsAndCancelTask();
            ResetAndGetAudioSource(out var audioSource);

            SetAudioSource(ref audioSource, req);
            _currentPreviewRequest = req;
            _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;

            _volumeTransporter.SetData(req);
            SetMixerAutoSuspend(_mixer, false);
            
            double startDspTime = AudioSettings.dspTime + AudioConstant.MixerWarmUpTime;
            audioSource.PlayScheduled(startDspTime);
            audioSource.SetScheduledEndTime(startDspTime + req.Duration);

            await Task.Delay(SecToMs(AudioConstant.MixerWarmUpTime), CancellationSource.Token);
            PlaybackIndicator.Start(selfLoop || replayData != null);
            _volumeTransporter.Start();
            EditorUtility.audioMasterMute = false;

            await Task.Delay(SecToMs(req.Duration), CancellationSource.Token);
            _volumeTransporter.End();

            if (selfLoop)
            {
                while (true)
                {
                    await AudioSourceReplay(req);
                }
            }
            else if (replayData != null)
            {
                while (true)
                {
                    req.SetReplay(replayData.NewReplay());
                    await Task.Delay(SecToMs(req.Duration), CancellationSource.Token);
                }
            }
            else
            {
                DestroyPreviewAudioSourceAndCancelTask();
            }
        }

        private void ResetAndGetAudioSource(out AudioSource result)
        {
            if (_currentEditorAudioSource)
            {
                CancelTask();
            }
            else
            {
                var gameObj = new GameObject("PreviewAudioClip");
                gameObj.tag = "EditorOnly";
                gameObj.hideFlags = HideFlags.HideAndDontSave;
                _currentEditorAudioSource = gameObj.AddComponent<AudioSource>();
            }
            result = _currentEditorAudioSource;
        }

        private void SetAudioSource(ref AudioSource audioSource, PreviewRequest req)
        {
            audioSource.clip = req.AudioClip;
            audioSource.playOnAwake = false;
            audioSource.timeSamples = GetSample(req.AudioClip.frequency, req.StartPosition);
            audioSource.pitch = req.Pitch;
            audioSource.outputAudioMixerGroup = GetEditorMasterTrack();
            audioSource.reverbZoneMix = 0f;
        }

        private async Task AudioSourceReplay(PreviewRequest req)
        {
            if (_currentEditorAudioSource != null)
            {
                PlaybackIndicator.End();
                if (_volumeTransporter.IsNewVolumeDifferentFromCurrent(req))
                {
                    _volumeTransporter.SetData(req);
                    await Task.Delay(SecToMs(AudioConstant.MixerWarmUpTime), CancellationSource.Token);
                }

                double dspTime = AudioSettings.dspTime;
                _currentEditorAudioSource.timeSamples = GetSample(req.AudioClip.frequency, req.StartPosition);
                _currentEditorAudioSource.PlayScheduled(dspTime);
                _currentEditorAudioSource.SetScheduledEndTime(dspTime + req.Duration);

                _volumeTransporter.Start();
                PlaybackIndicator.Start();
                await Task.Delay(SecToMs(_currentPreviewRequest.Duration), CancellationSource.Token);
            }
        }

        private void DestroyPreviewAudioSourceAndCancelTask()
        {
            SetMixerAutoSuspend(_mixer, true);
            if (_currentEditorAudioSource)
            {
                CancelTask();

                _currentEditorAudioSource.Stop();
                UnityEngine.Object.DestroyImmediate(_currentEditorAudioSource.gameObject);
                PlaybackIndicator.End();
                _volumeTransporter.End();
                _volumeTransporter.Dispose();
                _currentEditorAudioSource = null;
                TriggerOnFinished();
            }
        }
        #endregion

        #region AudioClip
        public void PlayClip(AudioClip audioClip, float startTime, float endTime, bool loop = false)
        {
            int startSample = AudioExtension.GetTimeSample(audioClip, startTime);
            int endSample = AudioExtension.GetTimeSample(audioClip, endTime);
            PlayClip(audioClip, startSample, endSample, loop);
        }

        public async void PlayClip(AudioClip audioClip, int startSample, int endSample, bool loop = false)
        {
            try
            {
                await PlayClipAsync(audioClip, startSample, endSample, loop);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipAsync(AudioClip audioClip, int startSample, int endSample, bool loop)
        {
            StopAllClips();

            _playPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            PlaybackIndicator.Start(loop);

            int sampleLength = audioClip.samples - startSample - endSample;
            int lengthInMs = (int)Math.Round(sampleLength / (double)audioClip.frequency * SecondInMilliseconds, MidpointRounding.AwayFromZero);

            await Task.Delay(lengthInMs, CancellationSource.Token);

            if (loop)
            {
                while (loop)
                {
                    await AudioClipReplay(audioClip, startSample, loop, lengthInMs);
                }
            }
            else
            {
                StopStaticPreviewClipsAndCancelTask();
            }
        }

        private async Task AudioClipReplay(AudioClip audioClip, int startSample, bool loop, int lengthInMs)
        {
            _stopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();

            _playPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            PlaybackIndicator.Start();

            await Task.Delay(lengthInMs, CancellationSource.Token);
        }

        private void StopStaticPreviewClipsAndCancelTask()
        {
            CancelTask();
            _stopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();
            TriggerOnFinished();
        }
        #endregion

        public void StopAllClips()
        {
            StopStaticPreviewClipsAndCancelTask();
            DestroyPreviewAudioSourceAndCancelTask();

            if (_previousMuteState != MuteState.None)
            {
                EditorUtility.audioMasterMute = _previousMuteState == MuteState.On;
                _previousMuteState = MuteState.None;
            }
        }

        private void TriggerOnFinished()
        {
            OnFinished?.Invoke();
            OnFinished = null;
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

        private void CancelTask()
        {
            if (_cancellationSource != null && _cancellationSource.Token.CanBeCanceled)
            {
                _cancellationSource.Cancel();
                _cancellationSource.Dispose();
                _cancellationSource = null;
            }
        }

        private void Dispose()
        {
            OnFinished = null;
            _currentPreviewRequest = null;
            _mixer = null;
            _volumeTransporter.Dispose();
            _volumeTransporter = null;
            StopStaticPreviewClipsAndCancelTask();
            DestroyPreviewAudioSourceAndCancelTask();
            SetMixerAutoSuspend(_mixer, true);
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

        private static T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
        {
            Type audioUtilClass = GetUnityEditorClass(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            return Delegate.CreateDelegate(typeof(T), method) as T;
        }

        private AudioMixerGroup GetEditorMasterTrack()
        {
            if (_masterTrack == null)
            {
                var tracks = _mixer != null ? _mixer.FindMatchingGroups("Master") : null;
                if (tracks != null && tracks.Length > 0)
                {
                    _masterTrack = tracks[0];
                }
                else
                {
                    Debug.LogError("Can't find EditorBroAudioMixer's Master audioMixerGroup, the fading and extra volume is not applied to the preview");
                }
            }
            return _masterTrack;
        }

        private static void SetMixerAutoSuspend(AudioMixer mixer, bool enable)
        {
            if(mixer)
            {
                SerializedObject serializedMixer = new SerializedObject(mixer);
                serializedMixer.Update();
                serializedMixer.FindProperty(MixerSuspendFieldName).boolValue = enable;
                serializedMixer.ApplyModifiedProperties();
            }
        }
    }
}
#endif