using System;
using System.Threading.Tasks;
using Ami.Extension;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Tools;
using static Ami.Extension.TimeExtension;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Editor
{
    public partial class AudioSourcePreviewStrategy : IDisposable
    {
        public enum MuteState { None, On, Off }
        public const string MixerSuspendFieldName = "m_EnableSuspend";

        // Not 100% sure but highly suspect the delay comes from the dsp time
        // The worst case might be 4096 sample in 44.1KHz, which is ~0.09 sec per chunk
        public const float MixerUpdateTime = 0.1f;

        private AudioSource _currentAudioSource = null;
        private AudioSource[] _audioSources = new AudioSource[2];
        private int _currentAudioSourceIndex = 0;
        private PreviewData _currentPlayingClip = null;
        private bool _isTransportIgnored = false;
        private bool _isRecursionOutside = false;

        private IEditorPreviewModule _mainModule;
        private EditorVolumeTransporter _volumeTransporter = null;
        private MuteState _previousMuteState = MuteState.None;
        private AudioMixer _mixer = null;
        private AudioMixerGroup _masterTrack = null;

        public AudioSourcePreviewStrategy(IEditorPreviewModule mainModule)
        {
            _mixer = Resources.Load<AudioMixer>(BroEditorUtility.EditorAudioMixerPath);
            if (!_mixer)
            {
                Debug.LogError($"Fail to load {BroName.EditorAudioMixerName}.mixer!");
                return;
            }

            _volumeTransporter = new EditorVolumeTransporter(_mixer);
            _mainModule = mainModule;
        }

        public void Stop()
        {
            _isRecursionOutside = false;
            DestroyPreviewAudioSourceAndCancelTask();
            if (_previousMuteState != MuteState.None)
            {
                EditorUtility.audioMasterMute = _previousMuteState == MuteState.On;
                _previousMuteState = MuteState.None;
            }
            _currentPlayingClip = null;
        }

        public void UpdatePreviewClipValues(float volume, float pitch, ITransport transport)
        {
            if (_currentPlayingClip != null)
            {
                _currentPlayingClip.Volume = volume;
                if (!_isTransportIgnored && transport != null)
                {
                    _currentPlayingClip.SetTransport(transport);
                }

                if (!Mathf.Approximately(_currentPlayingClip.Pitch, pitch))
                {
                    if (_currentAudioSource)
                    {
                        _currentAudioSource.pitch = pitch;
                    }
                    _currentPlayingClip.Pitch = pitch;
                }
            }
        }

        public async Task PlayAsync(PreviewData clip, bool selfLoop, ReplayData replayData, bool isTransportIgnored)
        {
            if (clip.AudioClip == null)
            {
                return;
            }

            if (!replayData.IsReplaying)
            {
                _isTransportIgnored = isTransportIgnored;
                SetOrResetAudioSource();

                _currentPlayingClip = clip;
                _previousMuteState = EditorUtility.audioMasterMute ? MuteState.On : MuteState.Off;
                EditorUtility.audioMasterMute = false;
                SetMixerAutoSuspend(_mixer, false);
            }

            _volumeTransporter.SetData(clip);
            SetAudioSourceData(clip);

            if (!replayData.IsReplaying)
            {
                double startDspTime = AudioSettings.dspTime + MixerUpdateTime;
                _currentAudioSource.PlayScheduled(startDspTime);
                await Task.Delay(SecToMs(MixerUpdateTime), _mainModule.CancellationSource.Token);
            }

            _mainModule.PlaybackIndicator.Start(selfLoop);
            _volumeTransporter.Start();

            await WaitUntilAudioSourceEnd(clip);

            _isRecursionOutside = replayData.OnReplay != null;
            if (_isRecursionOutside)
            {
                replayData.IsReplaying = true;
                replayData.OnReplay.Invoke(replayData);
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

        private void SetOrResetAudioSource()
        {
            if (_currentAudioSource)
            {
                _mainModule.CancelTask();
                _currentAudioSource.Stop();
            }
            else
            {
                var gameObj = new GameObject("PreviewAudioClip");
                gameObj.tag = "EditorOnly";
                gameObj.hideFlags = HideFlags.HideAndDontSave;
                _currentAudioSource = gameObj.AddComponent<AudioSource>();
                _currentAudioSource.outputAudioMixerGroup = GetEditorMasterTrack();
                _currentAudioSource.reverbZoneMix = 0f;
                _currentAudioSource.playOnAwake = false;
            }
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

        private void SetAudioSourceData(PreviewData clip)
        {
            if (_currentAudioSource)
            {
                _currentAudioSource.clip = clip.AudioClip;
                _currentAudioSource.timeSamples = GetSample(clip.AudioClip.frequency, clip.StartPosition);
                _currentAudioSource.pitch = clip.Pitch;
            }
        }

        private async Task AudioSourceReplay(PreviewData clip)
        {
            if (_currentAudioSource != null)
            {
                _mainModule.PlaybackIndicator.End();
                _volumeTransporter.End();
                if (_volumeTransporter.IsNewVolumeDifferentFromCurrent(clip))
                {
                    _volumeTransporter.SetData(clip);
                }

                _currentAudioSource.timeSamples = GetSample(clip.AudioClip.frequency, clip.StartPosition);
                _mainModule.PlaybackIndicator.Start();
                _volumeTransporter.Start();

                await WaitUntilAudioSourceEnd(clip);
            }
        }

        private async Task WaitUntilAudioSourceEnd(PreviewData clip)
        {
            do
            {
                Debug.Log($"sample:{_currentAudioSource.timeSamples} start:{clip.AbsoluteStartSamples} end:{clip.AbsoluteEndSamples}");
                await Task.Delay(1, _mainModule.CancellationSource.Token);
            }
            while (_currentAudioSource != null &&
            _currentAudioSource.timeSamples > clip.AbsoluteStartSamples &&
            _currentAudioSource.timeSamples < clip.AbsoluteEndSamples);
        }

        private void DestroyPreviewAudioSourceAndCancelTask()
        {
            SetMixerAutoSuspend(_mixer, true);
            if (_currentAudioSource)
            {
                _mainModule.CancelTask();

                _currentAudioSource.Stop();
                UnityEngine.Object.DestroyImmediate(_currentAudioSource.gameObject);
                _mainModule.PlaybackIndicator.End();
                _volumeTransporter.End();
                _volumeTransporter.Dispose();
                _currentAudioSource = null;
                if(!_isRecursionOutside)
                {
                    _mainModule.TriggerOnFinished();
                }
            }
            _isTransportIgnored = false;
        }

        private static void SetMixerAutoSuspend(AudioMixer mixer, bool enable)
        {
            if (mixer)
            {
                SerializedObject serializedMixer = new SerializedObject(mixer);
                serializedMixer.Update();
                serializedMixer.FindProperty(MixerSuspendFieldName).boolValue = enable;
                serializedMixer.ApplyModifiedProperties();
            }
        }

        public void Dispose()
        {
            _currentPlayingClip = null;
            _mixer = null;
            _volumeTransporter.Dispose();
            _volumeTransporter = null;
            DestroyPreviewAudioSourceAndCancelTask();
            SetMixerAutoSuspend(_mixer, true);
        }
    }
}