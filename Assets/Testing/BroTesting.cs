using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;
using UnityEngine.UI;
#if PACKAGE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;

#endif
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
        [SerializeField] float _delay = 0f;
        [SerializeField] float _offsetFromDSPTime = 0f;
#if PACKAGE_ADDRESSABLES
        [SerializeField] int _loadAssetIndex = 0; 
#endif

        private IAudioPlayer _player;

        public void Play() => _player = BroAudio.Play(_sound);
        public void PlayScheduled() => _player = BroAudio.Play(_sound).SetScheduledStartTime(AudioSettings.dspTime + _delay);

        public void SetVolume() => BroAudio.SetVolume(_volume, _fadeTime);
        public void SetVolume(BroAudioType audioType, int i) {}
        public void SetAudioTypeVolume() => BroAudio.SetVolume(_audioType, _volume, _fadeTime);
        public void SetSoundIDVolume() => BroAudio.SetVolume(_sound, _volume, _fadeTime);
        public void PlayerSetVolume() => _player.SetVolume(_volume, _fadeTime);

        public void SetPitch() => BroAudio.SetPitch(_pitch, _fadeTime);
        public void SetAudioTypePitch() => BroAudio.SetPitch(_audioType, _pitch, _fadeTime);
        public void PlayerSetPitch() => _player.SetPitch(_pitch, _fadeTime);

        public void SetScheduleStartTime() => _player.SetScheduledStartTime(AudioSettings.dspTime + _offsetFromDSPTime);
        public void SetScheduleEndTime() => _player.SetScheduledEndTime(AudioSettings.dspTime + _offsetFromDSPTime);
        public void SetDelay() => BroAudio.Play(_sound).SetDelay(_delay);

        public void PlayerStop() => _player.Stop(_fadeTime);
        public void StopAudioType() => BroAudio.Stop(_audioType, _fadeTime);
        public void StopSoundID() => BroAudio.Stop(_sound, _fadeTime);
        public void Pause() => _player.Pause();
        public void UnPause() => _player.UnPause();

#if !UNITY_WEBGL
        public void SetLowPassFilter() => BroAudio.SetEffect(Effect.LowPass(_frequency, _fadeTime), _audioType);
        public void ResetLowPassFilter() => BroAudio.SetEffect(Effect.ResetLowPass(_fadeTime), _audioType);
        public void SetHighPassFilter() => BroAudio.SetEffect(Effect.HighPass(_frequency, _fadeTime), _audioType);
        public void ResetHighPassFilter() => BroAudio.SetEffect(Effect.ResetHighPass(_fadeTime), _audioType);
        public void SetCustomEffect(string paraName) => BroAudio.SetEffect(Effect.Custom(paraName, _customEffectValue), _audioType);

        public void PlayerAsLowPassDominator() => _player.AsDominator().LowPassOthers(_frequency, _fading);
        public void PlayerAsHighPassDominator() => _player.AsDominator().HighPassOthers(_frequency, _fading); 
#endif

        public void AppendAsBGM() => _player.AsBGM().SetTransition(_transition, _fadeTime);
        public void PlayAsBGM() => _player = BroAudio.Play(_sound).AsBGM().SetTransition(_transition, _fadeTime);

#if PACKAGE_ADDRESSABLES
        public void LoadAllAssetAsync()
        {
            var handle = BroAudio.LoadAllAssetsAsync(_sound);
            handle.Completed += OnLoadAllAssetsFinished;

            void OnLoadAllAssetsFinished(AsyncOperationHandle<IList<AudioClip>> handle)
            {
                handle.Completed -= OnLoadAllAssetsFinished;
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("Failed to load all assets.");
                    return;
                }

                foreach (var clip in handle.Result)
                {
                    Debug.Log($"{clip.name} is loaded successfully!");
                }
            }
        }

        public void ReleaseAllAssets()
        {
            BroAudio.ReleaseAllAssets(_sound);
        }

        public void LoadAssetAsync()
        {
            var handle = BroAudio.LoadAssetAsync(_sound, _loadAssetIndex);
            handle.Completed += OnLoadAssetFinished;

            void OnLoadAssetFinished(AsyncOperationHandle<AudioClip> handle)
            {
                handle.Completed -= OnLoadAssetFinished;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("Failed to load all assets.");
                    return;
                }

                Debug.Log($"{handle.Result.name} is loaded successfully!");
            }
        }

        public void ReleaseAsset()
        {
            BroAudio.ReleaseAsset(_sound, _loadAssetIndex);
        }
#endif
    }
}