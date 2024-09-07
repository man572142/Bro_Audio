using Ami.Extension;
using UnityEngine;
using UnityEngine.UI;

namespace Ami.BroAudio.Testing
{
    public class BroTesting : MonoBehaviour
    {
        [SerializeField] SoundID _sound = default;

        [SerializeField] private Button _btn;

        [Space]
        [SerializeField] float _fadeTime = 0f;
        [SerializeField] BroAudioType _audioType = default;
        [SerializeField, Volume] float _volume = 1f;
        [SerializeField, Pitch] float _pitch = 1f;
        [SerializeField, Frequency] float _frequency = AudioConstant.MinFrequency;
        [SerializeField] float _customEffectValue = 0f;
        [SerializeField] Transition _transition = default;
        [SerializeField] Fading _fading = default;

        private IAudioPlayer _player;

        public void Play() => _player = BroAudio.Play(_sound);

        public void SetVolume() => BroAudio.SetVolume(_volume, _fadeTime);
        public void SetVolume(BroAudioType audioType, int i) {}
        public void SetAudioTypeVolume() => BroAudio.SetVolume(_audioType, _volume, _fadeTime);
        public void SetSoundIDVolume() => BroAudio.SetVolume(_sound, _volume, _fadeTime);
        public void PlayerSetVolume() => _player.SetVolume(_volume, _fadeTime);

        public void SetPitch() => BroAudio.SetPitch(_pitch, _fadeTime);
        public void SetAudioTypePitch() => BroAudio.SetPitch(_pitch, _audioType, _fadeTime);
        public void PlayerSetPitch() => _player.SetPitch(_pitch, _fadeTime);

        public void PlayerStop() => _player.Stop(_fadeTime);
        public void StopAudioType() => BroAudio.Stop(_audioType, _fadeTime);
        public void StopSoundID() => BroAudio.Stop(_sound, _fadeTime);

#if !UNITY_WEBGL
        public void SetLowPassFilter() => BroAudio.SetEffect(Effect.LowPass(_frequency, _fadeTime), _audioType);
        public void ResetLowPassFilter() => BroAudio.SetEffect(Effect.ResetLowPass(_fadeTime), _audioType);
        public void SetHighPassFilter() => BroAudio.SetEffect(Effect.HighPass(_frequency, _fadeTime), _audioType);
        public void ResetHighPassFilter() => BroAudio.SetEffect(Effect.ResetHighPass(_fadeTime), _audioType);
        public void SetCustomEffect(string paraName) => BroAudio.SetEffect(Effect.Custom(paraName, _customEffectValue), _audioType);

        public void PlayerAsLowPassDominator() => _player.AsDominator().LowPassOthers(_frequency, _fading);
        public void PlayerAsHighPassDominator() => _player.AsDominator().HighPassOthers(_frequency, _fading); 
#endif

        public void PlayerAsBGM() => _player.AsBGM().SetTransition(_transition, _fadeTime);
    }
}