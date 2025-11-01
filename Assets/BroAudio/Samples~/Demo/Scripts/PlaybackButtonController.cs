using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class PlaybackButtonController : InteractiveComponent
    {
        [Header("Pause Settings")]
        [SerializeField] private SoundID _target;
        [SerializeField] private float _fadeOut;

        [Header("Other Settings")]
        [SerializeField] private GameObject _pauseButton;
        [SerializeField] private GameObject _resumeButton;
        [SerializeField] private bool _isPauseOnStart;
        [Space]
        [SerializeField] private SoundID _switchSound;
        [SerializeField, Volume] private float _switchSoundVolume;
        [SerializeField, Pitch] private float _switchSoundPitch;
        [SerializeField] private float _switchSoundDuration;
        [SerializeField] private int _switchSoundVelocity;

        private bool _isPaused;

        private void SwitchPauseState()
        {
            if (_isPaused)
            {
                BroAudio.Pause(_target, _fadeOut);
            }
            else
            {
                BroAudio.UnPause(_target);
            }

            _pauseButton.SetActive(_isPaused);
            _resumeButton.SetActive(!_isPaused);
        }

        private void Start()
        {
            _isPaused = _isPauseOnStart;
            SwitchPauseState();
        }

        public override void OnInZoneChanged(bool isInZone)
        {
            base.OnInZoneChanged(isInZone);

            if(isInZone)
            {
                _isPaused = !_isPaused;
                SwitchPauseState();
                PlaySwitchSound();
            }
        }

        private void PlaySwitchSound()
        {
            BroAudio.Play(_switchSound)
                .SetVelocity(_switchSoundVelocity)
                .SetPitch(_switchSoundPitch)
                .SetVolume(_switchSoundVolume)
                .SetScheduledStartTime(AudioSettings.dspTime)
                .SetScheduledEndTime(AudioSettings.dspTime + _switchSoundDuration);
        }
    } 
}