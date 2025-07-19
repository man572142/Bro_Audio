using System;
using System.Collections;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio.Demo
{
    public class Cannon : InteractiveComponent
    {
        private const float MaxChargingLightIntensity = 5f;
        public event Action<float> OnForceChanged;
        public event Action OnCoolDownFinished;
        public event Action<PlaybackStage> OnChainedPlayModeStageChanged;

        [Header("Audio")]
        [Tooltip("This sound demonstrates the [Velocity] play mode")]
        [SerializeField] SoundID _fireSound = default;
        
        [Tooltip("This sound demonstrates the [Chained] play mode")]
        [SerializeField] SoundID _chargingSound = default;

        [Header("Cannonball")]
        [SerializeField] Transform _cannonball;
        [SerializeField] float _cannonballLifeTime = 3f;
        [SerializeField] ParticleSystem _particle;
        [SerializeField] private ParticleSystem _lightingParticle;
        [SerializeField, Min(1f)] float _coolDownTime = 1f;
        [SerializeField] Animator _animator;
        
        [Header("Firing")]
        [SerializeField] string _chargingAnimTrigger;
        [SerializeField] string _fireAnimTrigger;
        [SerializeField] float _chargingStartSoundDuration;
        [SerializeField] float _chargingEndSoundDuration;

        [Header("Force")]
        [SerializeField] float _forceIncrement = 1f;
        [SerializeField] AnimationCurve _forceIncrementCurve = null;
        [field: SerializeField] public float MaxForce { get; private set; }

        private float _force = 0;
        private float _charagingTime = 0f;
        private float _coolDownCountDownTime = 0f;
        private IAudioPlayer _chargingSoundPlayer = null;
        private float _maxCannonballScale = 1f;
        private Transform _firingCannonball;
        private Light _chargingLight = null;
        private Coroutine _playbackStageCoroutine = null;
        
        private void Start()
        {
            _maxCannonballScale = _cannonball.localScale.x;
            _cannonball.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsCoolingDown())
            {
                return;
            }
            else if (!InteractiveZone.IsInZone)
            {
                return;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                _charagingTime += Time.deltaTime;
                if (_force < MaxForce)
                {
                    _force = _forceIncrementCurve.Evaluate(_charagingTime / (MaxForce / _forceIncrement)) * MaxForce;
                    OnForceChanged?.Invoke(_force);
                }

                Charging();
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                Fire();
                _chargingSoundPlayer?.Stop(0.5f);
                this.SafeStopCoroutine(_playbackStageCoroutine);
                OnChainedPlayModeStageChanged?.Invoke(PlaybackStage.End);
            }
        }

        private void Charging()
        {
            if (_chargingSoundPlayer == null || !_chargingSoundPlayer.IsActive)
            {
                PlayChargingSound();
                _animator.SetTrigger(_chargingAnimTrigger);
            }

            if (!_particle.isPlaying)
            {
                _particle.Play();
            }

            if(_firingCannonball == null)
            {
                _firingCannonball = Instantiate(_cannonball, _cannonball.parent);
                _firingCannonball.gameObject.SetActive(true);
                _chargingLight = _firingCannonball.GetComponentInChildren<Light>();
            }

            float normalizedScale = (_force / MaxForce) * UnityEngine.Random.Range(0.2f, 1f);
            _firingCannonball.localScale = normalizedScale * _maxCannonballScale * Vector3.one;
            _chargingLight.intensity = normalizedScale * MaxChargingLightIntensity; 
        }

        private bool IsCoolingDown()
        {
            if(_coolDownCountDownTime > 0f)
            {
                _coolDownCountDownTime -= Time.deltaTime;
                if (_coolDownCountDownTime <= 0f)
                {
                    OnCoolDownFinished?.Invoke();
                }
                return true;
            }
            return false;
        }

        private void Fire()
        {
            if(_force <= 0f)
            {
                return;
            }

            _animator.SetTrigger(_fireAnimTrigger);
            FireCannonball();
            PlayFireSound((int)_force);
            _lightingParticle.Play();

            _force = 0f;
            _charagingTime = 0f;
            _coolDownCountDownTime = _coolDownTime;
            _particle.Stop();
        }

        private void FireCannonball()
        {
            _chargingLight.intensity = MaxChargingLightIntensity;
            _firingCannonball.GetComponentInChildren<ParticleSystem>().gameObject.SetActive(false);
            _firingCannonball.localScale = (_force / MaxForce) * _maxCannonballScale * Vector3.one;
            Rigidbody rigid = _firingCannonball.GetComponent<Rigidbody>();
            rigid.isKinematic = false;
            rigid.AddForce(_cannonball.transform.forward * _force, ForceMode.Impulse);
            Destroy(_firingCannonball.gameObject, _cannonballLifeTime);
            _firingCannonball = null;
        }

        #region Audio
        private void PlayFireSound(int velocity)
        {
            BroAudio.Play(_fireSound).SetVelocity(velocity);
        }

        private void PlayChargingSound()
        {
            _chargingSoundPlayer = BroAudio.Play(_chargingSound);
            OnChainedPlayModeStageChanged?.Invoke(PlaybackStage.Start);
            _playbackStageCoroutine = StartCoroutine(WaitForLoopStage());
            
            _chargingSoundPlayer.OnEnd(_ =>
            {
                OnChainedPlayModeStageChanged?.Invoke(PlaybackStage.None);
                _chargingSoundPlayer = null;
            });
        }
        #endregion

        private IEnumerator WaitForLoopStage()
        {
            yield return new WaitForSeconds(_chargingStartSoundDuration);
            OnChainedPlayModeStageChanged?.Invoke(PlaybackStage.Loop);
        }
    }
}