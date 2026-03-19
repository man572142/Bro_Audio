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
        private Ease _fadeInEase;
        private Ease _fadeOutEase;
        private readonly object[] _parameters;
        private readonly bool _isInitSuccessfully;
        private readonly MethodInfo _method;
        private PreviewRequest _currentReq;
        private float _playbackPos;
        private float _dbVolume;
        private float _currentLinearVolume;
        private bool _isCrossFadingOut;
        private float _crossFadeOutDuration;
        private float _crossFadeOutStartVolume;
        private float _crossFadeOutPos;
        private Ease _crossFadeOutEase;
        private bool _isCrossFadingIn;
        private bool _crossFadeInHandled;
        private float _crossFadeInDuration;
        private float _crossFadeInPos;
        private float _crossFadeInTargetVolume;
        private Ease _crossFadeInEase;

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
            if (_crossFadeInHandled)
            {
                // Cross-fade-in is in progress or just completed; update target volume with the correct value
                _crossFadeInTargetVolume = req.Volume;
            }
            else
            {
                SetStartVolume(req);
            }
        }

        public void SetStartVolume(PreviewRequest req)
        {
            float startVol = req.FadeIn > 0f ? 0f : req.Volume;
            SetVolume(startVol, true);
        }

        public override void Start()
        {
            if (_crossFadeInHandled)
            {
                // Cross-fade-in is already running; don't reset _playbackPos or re-subscribe
                _crossFadeInHandled = false;
                return;
            }
            _playbackPos = 0f;
            base.Start();
        }

        public override void Dispose()
        {
            _isCrossFadingOut = false;
            _isCrossFadingIn = false;
            _crossFadeInHandled = false;
            SetVolume(1f);
            base.Dispose();
        }

        public void BeginCrossFadeOut(float duration, Ease ease)
        {
            _isCrossFadingOut = true;
            _crossFadeOutDuration = duration;
            _crossFadeOutStartVolume = _currentLinearVolume;
            _crossFadeOutPos = 0f;
            _crossFadeOutEase = ease;
            Start();
        }

        public void BeginCrossFadeIn(float duration, Ease ease)
        {
            _isCrossFadingIn = true;
            _crossFadeInHandled = true;
            _crossFadeInDuration = duration;
            _crossFadeInPos = 0f;
            _crossFadeInTargetVolume = AudioConstant.FullVolume;
            _crossFadeInEase = ease;
            _playbackPos = 0f;
            SetVolume(0f, true);
            base.Start();
        }

        public void UseSeamlessEasing()
        {
            var setting = BroEditorUtility.RuntimeSetting;
            _fadeInEase = setting.SeamlessFadeInEase;
            _fadeOutEase = setting.SeamlessFadeOutEase;
        }

        protected override void Update()
        {
            if (!_isInitSuccessfully)
            {
                return;
            }

            if (_isCrossFadingOut)
            {
                _crossFadeOutPos += DeltaTime;
                if (_crossFadeOutPos >= _crossFadeOutDuration)
                {
                    SetVolume(0f);
                    _isCrossFadingOut = false;
                    base.Update();
                    Dispose();
                    return;
                }
                float t = (_crossFadeOutPos / _crossFadeOutDuration).SetEase(_crossFadeOutEase);
                SetVolume(Mathf.Lerp(_crossFadeOutStartVolume, 0f, t));
                base.Update();
                return;
            }

            if (_isCrossFadingIn)
            {
                _crossFadeInPos += DeltaTime;
                float targetVol = _crossFadeInTargetVolume;
                if (_crossFadeInPos >= _crossFadeInDuration)
                {
                    if (_currentReq == null)
                    {
                        // Hold at target until Init provides _currentReq
                        SetVolume(targetVol);
                        base.Update();
                        return;
                    }
                    SetVolume(targetVol);
                    _isCrossFadingIn = false;
                    _playbackPos = _crossFadeInDuration;
                    base.Update();
                    return;
                }
                float fadeT = (_crossFadeInPos / _crossFadeInDuration).SetEase(_crossFadeInEase);
                SetVolume(Mathf.Lerp(0f, targetVol, fadeT));
                base.Update();
                return;
            }

            if (_currentReq == null)
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

            _currentLinearVolume = vol;
            _dbVolume = db;
            _parameters[2] = db;
            _method?.Invoke(_mixerGroup, _parameters);
        }
    }
}
