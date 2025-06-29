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
        private PreviewRequest _currentReq;
        private float _elapsedTime;
        private float _dbVolume;
        private float _speed = 1f;
        
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

        public void SetData(PreviewRequest req)
        {
            _currentReq = req;
            _speed = req.Pitch;

            if (_isInitSuccessfully && _method == null)
            {
                var reflection = new ClassReflectionHelper();
                const string methodName = nameof(BroAudioReflection.MethodName.SetValueForVolume);
                _method = reflection.MixerGroupClass.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            }

            float startVol = GetStartVolume(req);
            SetVolume(startVol, true);
        }

        private static float GetStartVolume(PreviewRequest req)
        {
            return req.FadeIn > 0f ? 0f : req.Volume;
        }

        public bool IsNewVolumeDifferentFromCurrent(PreviewRequest req)
        {
            float startVol = GetStartVolume(req);
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

            var fadeOutPos = _currentReq.Duration - _currentReq.FadeOut;
            bool hasFadeOut = _currentReq.FadeOut > 0f;

            _elapsedTime += DeltaTime * _speed;
            if (_elapsedTime < _currentReq.FadeIn)
            {
                float t = (_elapsedTime / _currentReq.FadeIn).SetEase(_fadeInEase);
                SetVolume(Mathf.Lerp(0f, _currentReq.Volume, t));
            }
            else if (hasFadeOut && _elapsedTime >= fadeOutPos && _elapsedTime < _currentReq.Duration)
            {
                float t = ((float)(_elapsedTime - fadeOutPos) / _currentReq.FadeOut).SetEase(_fadeOutEase);
                SetVolume(Mathf.Lerp(_currentReq.Volume, 0f, t));
            }
            else
            {
                SetVolume(hasFadeOut && _elapsedTime >= _currentReq.Duration ? 0f : _currentReq.Volume);
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