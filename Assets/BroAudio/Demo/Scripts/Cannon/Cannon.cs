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
        public event Action OnReloaded;

        [SerializeField] SoundID _fireSound = default;
        [SerializeField] SoundID _chargingSound = default;

        [SerializeField] Rigidbody _cannonball = null;
        [SerializeField, Min(1f)] float _reloadTime = 3f;
        
        [SerializeField] private float _forceIncrement = 1f;
        [field: SerializeField] public float MaxForce { get; private set; }

        private float _force = 0;
        private float _countDownTime = 0f;
        private Vector3 _cannonballOriginalPos = default;
        private Quaternion _cannonballOriginalRotation = default;

        private IAudioPlayer _chargingSoundPlayer = null;

        private void Start()
        {
            _cannonballOriginalPos = _cannonball.transform.position;
            _cannonballOriginalRotation = _cannonball.transform.rotation;
        }

        private void Update()
        {
            if (_countDownTime > 0f)
            {
                CountDownToReload();
                return;
            }

            if (InteractiveZone.IsInZone)
            {
                if (Input.GetKey(KeyCode.Space))
                {
                    _force += _forceIncrement * Time.deltaTime;
                    _force = Mathf.Min(_force, MaxForce);
                    OnForceChanged?.Invoke(_force);

                    if(_chargingSoundPlayer == null || !_chargingSoundPlayer.IsActive)
                    {
                        PlayChargingSound();
                    }
                }
                else if (Input.GetKeyUp(KeyCode.Space))
                {
                    Fire();
                    _chargingSoundPlayer?.Stop(0.5f);
                }
            }
        }

        private void CountDownToReload()
        {
            _countDownTime -= Time.deltaTime;

            if(_countDownTime <= 0f)
            {
                ReloadCannonball();
            }
        }

        private void ReloadCannonball()
        {
            _cannonball.isKinematic = true;
            _cannonball.transform.position = _cannonballOriginalPos;
            _cannonball.transform.rotation = _cannonballOriginalRotation;
            OnReloaded?.Invoke();
        }

        private void Fire()
        {
            if(_force > 0f)
            {
                _cannonball.isKinematic = false;
                _cannonball.AddForce(_cannonball.transform.forward * _force, ForceMode.Impulse);
                OnFire?.Invoke(_force);

                PlayFireSound((int)_force);

                _force = 0f;
                OnForceChanged?.Invoke(0f);

                _countDownTime = _reloadTime;
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
            _chargingSoundPlayer.OnEndPlaying += DisposeChargingSoundPlayer;
        }

        private void DisposeChargingSoundPlayer(SoundID iD)
        {
            _chargingSoundPlayer.OnEndPlaying -= DisposeChargingSoundPlayer;
            _chargingSoundPlayer = null;
        } 
        #endregion
    }
}