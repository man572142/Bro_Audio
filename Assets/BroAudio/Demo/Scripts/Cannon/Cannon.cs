using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class Cannon : InteractiveComponent
    {
        public event Action<float> OnForceChanged;
        public event Action<float> OnFire;
        public event Action OnCoolDownFinished;

        [Header("Audio")]
        [SerializeField] SoundID _fireSound = default;
        [SerializeField] SoundID _chargingSound = default;

        [Header("Cannonball")]
        [SerializeField] Transform _cannonball = null;
        [SerializeField] float _cannonballLifeTime = 3f;
        [SerializeField] ParticleSystem _particle = null;
        [SerializeField, Min(1f)] float _coolDownTime = 1f;

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
        private const float MaxChargingLightIntensity = 5f;

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
                _force = _forceIncrementCurve.Evaluate(_charagingTime / (MaxForce / _forceIncrement)) * MaxForce;
                OnForceChanged?.Invoke(_force);
                if (_force >= MaxForce)
                {
                    _force = MaxForce;
                    Fire();
                    return;
                }

                Charging();
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                Fire();
                _chargingSoundPlayer?.Stop(0.5f);
            }
        }

        private void Charging()
        {
            if (_chargingSoundPlayer == null || !_chargingSoundPlayer.IsActive)
            {
                PlayChargingSound();
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
            _firingCannonball.localScale = Vector3.one * normalizedScale * _maxCannonballScale;
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

            FireCannonball();
            OnFire?.Invoke(_force);

            PlayFireSound((int)_force);
            _chargingSoundPlayer.Stop(0.5f);

            _force = 0f;
            _charagingTime = 0f;
            _coolDownCountDownTime = _coolDownTime;
            _particle.Stop();

            void FireCannonball()
            {
                _chargingLight.intensity = MaxChargingLightIntensity;
                _firingCannonball.GetComponentInChildren<ParticleSystem>().gameObject.SetActive(false);
                _firingCannonball.localScale = Vector3.one * (_force / MaxForce) * _maxCannonballScale;
                Rigidbody rigidbody = _firingCannonball.GetComponent<Rigidbody>();
                rigidbody.isKinematic = false;
                rigidbody.AddForce(_cannonball.transform.forward * _force, ForceMode.Impulse);
                Destroy(_firingCannonball.gameObject, _cannonballLifeTime);
                _firingCannonball = null;
            }
        }

        #region Audio
        private void PlayFireSound(int velocity)
        {
            BroAudio.Play(_fireSound).SetVelocity(velocity);
        }

        private void PlayChargingSound()
        {
            _chargingSoundPlayer = BroAudio.Play(_chargingSound);
            _chargingSoundPlayer.OnEnd(_ => _chargingSoundPlayer = null);
        }
        #endregion
    }
}