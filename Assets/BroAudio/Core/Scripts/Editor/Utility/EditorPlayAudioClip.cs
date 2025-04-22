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
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tools;

#if UNITY_EDITOR
namespace Ami.Extension
{
    public class EditorPlayAudioClip
    {
        public class Data
        {
            public AudioClip AudioClip;
            public float Volume = AudioConstant.FullVolume;
            public float Pitch = AudioConstant.DefaultPitch;
            public float StartPosition;
            public float EndPosition;
            public float FadeIn;
            public float FadeOut;

            public Data(AudioClip audioClip, float volume, float pitch, ITransport transport)
            {
                AudioClip = audioClip;
                Volume = volume;
                Pitch = pitch;
                SetTransport(transport);
            }

            public Data(IBroAudioClip broAudioClip, float pitch)
            {
                AudioClip = broAudioClip.GetAudioClip();
                Volume = broAudioClip.Volume;
                Pitch = pitch;
                StartPosition = broAudioClip.StartPosition;
                EndPosition = broAudioClip.EndPosition;
                FadeIn = broAudioClip.FadeIn;
                FadeOut = broAudioClip.FadeOut;
            }

            public void SetTransport(ITransport transport)
            {
                StartPosition = transport.StartPosition;
                EndPosition = transport.EndPosition;
                FadeIn = transport.FadeIn;
                FadeOut = transport.FadeOut;
            }

            public float Duration => (AudioClip.length - EndPosition - StartPosition) / Pitch;
        }
        public enum MuteState { None, On, Off }

        public delegate void PlayPreviewClip(AudioClip audioClip, int startSample, bool loop);
        public delegate void StopAllPreviewClips();

        public const string MixerSuspendFieldName = "m_EnableSuspend";
        public const string IgnoreSettingTooltip = "Right-click to play the audio clip directly";
        public const string PlayClipMethodName = "PlayPreviewClip";
        public const string StopClipMethodName = "StopAllPreviewClips";

        // Not 100% sure but highly suspect the delay comes from the dsp time
        // The worst case might be 4096 sample in 44.1KHz, which is ~0.09 sec per chunk
        public const float MixerUpdateTime = 0.1f;

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
        public StopAllPreviewClips StopAllPreviewClipsDelegate = null;
        public PlayPreviewClip PlayPreviewClipDelegate = null;

        private CancellationTokenSource _cancellationSource = new CancellationTokenSource();
        private AudioSource _currentEditorAudioSource = null;
        private Data _currentPlayingClip = null;
        private bool _isRecursionOutside = false;
        private AudioMixer _mixer = null;
        private AudioMixerGroup _masterTrack = null;
        private EditorAudioPreviewer _volumeTransporter = null;
        private MuteState _previousMuteState = MuteState.None;

        private CancellationTokenSource CancellationSource => _cancellationSource ??= new CancellationTokenSource();

        public EditorPlayAudioClip()
        {
            _mixer = Resources.Load<AudioMixer>(BroEditorUtility.EditorAudioMixerPath);
            if(!_mixer)
            {
                Debug.LogError($"Fail to load {BroName.EditorAudioMixerName}.mixer!");
                return;
            }

            PlaybackIndicator = new PlaybackIndicatorUpdater();
            _volumeTransporter = new EditorAudioPreviewer(_mixer);

            StopAllPreviewClipsDelegate = GetAudioUtilMethodDelegate<StopAllPreviewClips>(StopClipMethodName);
            PlayPreviewClipDelegate = GetAudioUtilMethodDelegate<PlayPreviewClip>(PlayClipMethodName);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public void UpdatePreviewClipValues(float volume, float pitch, ITransport transport)
        {
            if(_currentPlayingClip != null)
            {
                _currentPlayingClip.Volume = volume;
                if(transport != null)
                {
                    _currentPlayingClip.SetTransport(transport);
                }

                if (!Mathf.Approximately(_currentPlayingClip.Pitch, pitch))
                {
                    if (_currentEditorAudioSource)
                    {
                        _currentEditorAudioSource.pitch = pitch;
                    }
                    _currentPlayingClip.Pitch = pitch;
                }
            }
        }

        #region AudioSource
        public async void PlayClipByAudioSource(Data clip, bool selfLoop = false, Action onReplay = null)
        {
            try
            {
                await PlayClipByAudioSourceAsync(clip, selfLoop, onReplay);
            }
            catch (OperationCanceledException) { }
        }

        private async Task PlayClipByAudioSourceAsync(Data clip, bool selfLoop, Action onReplay)
        {
            if(clip.AudioClip == null)
            {
                return;
            }
            StopStaticPreviewClipsAndCancelTask();
            ResetAndGetAudioSource(out var audioSource);

            SetAudioSource(ref audioSource, clip);
            _currentPlayingClip = clip;
            _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;

            _volumeTransporter.SetData(clip);
            SetMixerAutoSuspend(_mixer, false);
            
            double startDspTime = AudioSettings.dspTime + MixerUpdateTime;
            audioSource.PlayScheduled(startDspTime);
            audioSource.SetScheduledEndTime(startDspTime + clip.Duration);

            await Task.Delay(SecToMs(MixerUpdateTime), CancellationSource.Token);
            PlaybackIndicator.Start(selfLoop);
            _volumeTransporter.Start();
            EditorUtility.audioMasterMute = false;

            await Task.Delay(SecToMs(clip.Duration), CancellationSource.Token);
            _volumeTransporter.End();

            _isRecursionOutside = onReplay != null;
            if (_isRecursionOutside)
            {
                onReplay.Invoke();
                return;
            }

            if (selfLoop)
            {
                while (selfLoop)
                {
                    await AudioSourceReplay(clip);
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

        private void SetAudioSource(ref AudioSource audioSource, Data clip)
        {
            audioSource.clip = clip.AudioClip;
            audioSource.playOnAwake = false;
            // TODO: bug
            Debug.Log($"start:{clip.StartPosition} vol:{clip.Volume} pitch:{clip.Pitch}");
            audioSource.timeSamples = GetSample(clip.AudioClip.frequency, clip.StartPosition);
            audioSource.pitch = clip.Pitch;
            audioSource.outputAudioMixerGroup = GetEditorMasterTrack();
            audioSource.reverbZoneMix = 0f;
        }

        private async Task AudioSourceReplay(Data clip)
        {
            if (_currentEditorAudioSource != null)
            {
                PlaybackIndicator.End();
                if (_volumeTransporter.IsNewVolumeDifferentFromCurrent(clip))
                {
                    _volumeTransporter.SetData(clip);
                    await Task.Delay(SecToMs(MixerUpdateTime), CancellationSource.Token);
                }

                double dspTime = AudioSettings.dspTime;
                _currentEditorAudioSource.timeSamples = GetSample(clip.AudioClip.frequency, clip.StartPosition);
                _currentEditorAudioSource.PlayScheduled(dspTime);
                _currentEditorAudioSource.SetScheduledEndTime(dspTime + clip.Duration);

                _volumeTransporter.Start();
                PlaybackIndicator.Start();
                await Task.Delay(SecToMs(_currentPlayingClip.Duration), CancellationSource.Token);
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

            PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
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
            StopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();

            PlayPreviewClipDelegate.Invoke(audioClip, startSample, loop);
            PlaybackIndicator.Start();

            await Task.Delay(lengthInMs, CancellationSource.Token);
        }

        private void StopStaticPreviewClipsAndCancelTask()
        {
            CancelTask();
            StopAllPreviewClipsDelegate.Invoke();
            PlaybackIndicator.End();
            TriggerOnFinished();
        }
        #endregion

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
            _currentPlayingClip = null;
        }

        private void TriggerOnFinished()
        {
            if (!_isRecursionOutside)
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
            _currentPlayingClip = null;
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

        private T GetAudioUtilMethodDelegate<T>(string methodName) where T : Delegate
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