using System.Linq;
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
        private readonly MethodInfo _method;
        private PreviewRequest _currentReq;
        private float _playbackPos;
        private float _dbVolume;
        
        public EditorVolumeTransporter(AudioMixer mixer, string trackName)
        {
            AudioMixerSnapshot snapshot = mixer.FindSnapshot(DefaultSnapshotName);
            _mixerGroup = mixer.FindMatchingGroups(trackName).FirstOrDefault();
            _fadeInEase = BroEditorUtility.RuntimeSetting.DefaultFadeInEase;
            _fadeOutEase = BroEditorUtility.RuntimeSetting.DefaultFadeOutEase;
            _isInitSuccessfully = mixer && snapshot && _mixerGroup;

            if(_isInitSuccessfully)
            {
                _parameters = new object[] { mixer, snapshot, 0f };
                var reflection = new ClassReflectionHelper();
                const string methodName = nameof(BroAudioReflection.MethodName.SetValueForVolume);
                _method = reflection.MixerGroupClass.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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

        public void Init(PreviewRequest req)
        {
            _currentReq = req;
            SetStartVolume(req);
        }

        public void SetStartVolume(PreviewRequest req)
        {
            float startVol = req.FadeIn > 0f ? 0f : req.Volume;
            SetVolume(startVol, true);
        }

        public override void Start()
        {
            _playbackPos = 0f;
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

            var fadeOutPos = _currentReq.NonPitchDuration - _currentReq.FadeOut;
            bool hasFadeOut = _currentReq.FadeOut > 0f;

            _playbackPos += DeltaTime * _currentReq.Pitch;
            if (_playbackPos < _currentReq.FadeIn)
            {
                float t = (_playbackPos / _currentReq.FadeIn).SetEase(_fadeInEase);
                SetVolume(Mathf.Lerp(0f, _currentReq.Volume, t));
            }
            else if (hasFadeOut && _playbackPos >= fadeOutPos && _playbackPos < _currentReq.NonPitchDuration)
            {
                float t = ((float)(_playbackPos - fadeOutPos) / _currentReq.FadeOut).SetEase(_fadeOutEase);
                SetVolume(Mathf.Lerp(_currentReq.Volume, 0f, t));
            }
            else
            {
                SetVolume(hasFadeOut && _playbackPos >= _currentReq.NonPitchDuration ? 0f : _currentReq.Volume);
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