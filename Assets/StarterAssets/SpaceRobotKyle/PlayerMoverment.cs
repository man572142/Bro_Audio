using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class PlayerMoverment : MonoBehaviour
    {
        public const string MovingSpeed = "Speed";
        public const string MotionSpeed = "MotionSpeed";
        public const string Jump = "Jump";
        public const string Grounded = "Grounded";
        public const string FreeFall = "FreeFall";

        [SerializeField] private Animator _animator = null;
        [SerializeField] private float _movingSpeed = 2f;
        [SerializeField] private CharacterController _characterController = null;
        [SerializeField] private Transform _cameraTarget = null;
        [SerializeField] private float _rotationSensitivity = 5f;

        [SerializeField] private float _maxPitchAngle = 60f;
        [SerializeField] private float _minPitchAngle = -20f;

        private float _rotationX = 0f;
        private float _rotationY = 0f;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {


            float mouseX = Input.GetAxis("Mouse X") * _rotationSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _rotationSensitivity;

            _rotationX = ClampAngle(_rotationX + mouseY, _minPitchAngle, _maxPitchAngle);
            _rotationY = ClampAngle(_rotationY + mouseX, float.MinValue, float.MaxValue);
            _cameraTarget.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);

            transform.rotation = Quaternion.Euler(0f, _rotationY, 0f);

            Moving();
        }

        float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void Moving()
        {
            float moveForward = Input.GetAxis("Vertical");
            float moveX = Input.GetAxis("Horizontal");
            _animator.SetFloat(MotionSpeed, moveForward < 0 ? -moveForward : moveForward);
            _animator.SetFloat(MovingSpeed, moveForward * _movingSpeed);
            _characterController.SimpleMove(transform.forward * moveForward * _movingSpeed);
        }

        public void OnFootstep()
        {

        }
    }

}