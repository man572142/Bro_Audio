using UnityEngine;
using Ami.Extension;
using UnityEngine.Audio;
using Ami.Extension.Reflection;
using System.Reflection;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    public class EditorVolumeTransporter : EditorUpdateHelper
    {
        private const string DefaultSnapshotName = "Snapshot";

        private readonly AudioMixerGroup _mixerGroup;
        private readonly Ease _fadeInEase;
        private readonly Ease _fadeOutEase;
        private readonly object[] _parameters;
        private readonly bool _isInitSuccessfully;
        
        private MethodInfo _method;
        private EditorPlayAudioClip.Data _clipData;
        private float _elapsedTime;
        private float _dbVolume;
        private float _pitch = 1f;
        
        public EditorVolumeTransporter(AudioMixer mixer)
        {
            AudioMixerSnapshot snapshot = mixer.FindSnapshot(DefaultSnapshotName);
            var tracks = mixer.FindMatchingGroups(BroName.MasterTrackName);
            _mixerGroup = tracks != null && tracks.Length > 0 ? tracks[0] : null;
            _fadeInEase = BroEditorUtility.RuntimeSetting.DefaultFadeInEase;
            _fadeOutEase = BroEditorUtility.RuntimeSetting.DefaultFadeOutEase;
            _isInitSuccessfully = mixer && snapshot && _mixerGroup;

            if(_isInitSuccessfully)
            {
                _parameters = new object[] { mixer, snapshot, 0f };
            }
            else
            {
                Debug.LogError($"EditorAudioPreviewer initializing fail! make sure you have " +
                    $"{BroName.EditorAudioMixerName}.mixer in Resources/Editor folder, " +
                    $"an AudioMixerGroup named:{BroName.MasterTrackName}, " +
                    $"and a snapshot named:{DefaultSnapshotName}");
            }
        }

        protected override float UpdateInterval => 1 / 30f;

        public void SetData(EditorPlayAudioClip.Data clipData, float pitch = 1f)
        {
            _clipData = clipData;
            _pitch = pitch;

            if (_isInitSuccessfully && _method == null)
            {
                var reflection = new ClassReflectionHelper();
                const string methodName = nameof(BroAudioReflection.MethodName.SetValueForVolume);
                _method = reflection.MixerGroupClass.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            float startVol = GetStartVolume(clipData);
            SetVolume(startVol, true);
        }

        private static float GetStartVolume(EditorPlayAudioClip.Data clipData)
        {
            return clipData.FadeIn > 0f ? 0f : clipData.Volume;
        }

        public bool IsNewVolumeDifferentFromCurrent(EditorPlayAudioClip.Data clipData)
        {
            float startVol = GetStartVolume(clipData);
            return !Mathf.Approximately(startVol.ToDecibel(), _dbVolume);
        }

        public override void Start()
        {
            _elapsedTime = 0f;
            base.Start();
        }

        public override void Dispose()
        {
            SetVolume(1f);
            base.Dispose();
        }

        protected override void Update()
        {
            if (!_isInitSuccessfully)
            {
                return;
            }

            float fadeOutPos = _clipData.Duration - _clipData.FadeOut;
            bool hasFadeOut = _clipData.FadeOut > 0f;

            _elapsedTime += DeltaTime * _pitch;
            if (_elapsedTime < _clipData.FadeIn)
            {
                float t = (_elapsedTime / _clipData.FadeIn).SetEase(_fadeInEase);
                SetVolume(Mathf.Lerp(0f, _clipData.Volume, t));
            }
            else if (hasFadeOut && _elapsedTime >= fadeOutPos && _elapsedTime < _clipData.Duration)
            {
                float t = ((_elapsedTime - fadeOutPos) / _clipData.FadeOut).SetEase(_fadeOutEase);
                SetVolume(Mathf.Lerp(_clipData.Volume, 0f, t));
            }
            else
            {
                SetVolume(hasFadeOut && _elapsedTime >= _clipData.Duration ? 0f : _clipData.Volume);
            }
            base.Update();
        }

        private void SetVolume(float vol, bool forceSet = false)
        {
            if (!_isInitSuccessfully)
            {
                return;
            }

            float db = vol.ToDecibel();
            if (!forceSet && Mathf.Approximately(db, _dbVolume))
            {
                return;
            }

            _dbVolume = db;
            _parameters[2] = db;
            _method?.Invoke(_mixerGroup, _parameters);
        }
    }
}