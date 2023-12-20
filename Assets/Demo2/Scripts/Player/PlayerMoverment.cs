using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Demo
{
    public class PlayerMoverment : MonoBehaviour
    {
        public const string MovingSpeedPara = "Speed";
        public const string MotionSpeedPara = "MotionSpeed";
        public const string JumpPara = "Jump";
        public const string GroundedPara = "Grounded";
        public const string FreeFallPara = "FreeFall";

        private const float DefaultInputSensitivity = 3f;

        [SerializeField] private Animator _animator = null;
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private CharacterController _characterController = null;
        [SerializeField] private Transform _cameraTarget = null;
        [SerializeField] private Camera _camera = null;
        [SerializeField] private float _pitchRotationSensitivity = 1f;
        [SerializeField] private float _yawRotationSensitivity = 1.5f;

        [SerializeField] private float _maxPitchAngle = 60f;
        [SerializeField] private float _minPitchAngle = -20f;

        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private bool _canControl = true;

        private Coroutine _forceWalkCoroutine = null;

        public float WalkSpeed => _walkSpeed;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

		private void Update()
        {
            if(!_canControl || PauseMenu.Instance.IsOpen)
			{
                return;
			}

            CameraRotation();
            Moving();
        }

        // Triggered by timeline system
        public void SetForceWalkSpeedRatio(float speedRatio)
		{
            if(_forceWalkCoroutine != null)
			{
                StopCoroutine(_forceWalkCoroutine);
			}

            if(speedRatio != 0f)
			{
                _canControl = false;
                _forceWalkCoroutine = StartCoroutine(ForceWalk(_walkSpeed * speedRatio));
            }
            else
            {
                _animator.SetFloat(MotionSpeedPara, 0f);
                _animator.SetFloat(MovingSpeedPara, 0f);
                _canControl = true;
            }
        }

        private IEnumerator ForceWalk(float targetSpeed)
		{
            float speed = Math.Min(_animator.GetFloat(MovingSpeedPara),_walkSpeed);
            while(true)
			{
                if(speed != targetSpeed)
				{
                    bool isSpeedUp = targetSpeed > speed;
                    speed += DefaultInputSensitivity * Time.deltaTime * (isSpeedUp ? 1f : -1f);
                    if((isSpeedUp && speed > targetSpeed) || (!isSpeedUp && speed < targetSpeed))
					{
                        speed = targetSpeed;
					}
                }

				_animator.SetFloat(MotionSpeedPara, Mathf.Clamp01(speed.SetEase(Ease.OutQuint)));
				_animator.SetFloat(MovingSpeedPara, speed);
				Vector3 direction = transform.TransformDirection(Vector3.forward);
                _characterController.SimpleMove(direction * speed);
                yield return null;
            }   
        }

        private void CameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * _yawRotationSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _pitchRotationSensitivity;

            _rotationX = ClampAngle(_rotationX - mouseY, _minPitchAngle, _maxPitchAngle);
            _rotationY = ClampAngle(_rotationY + mouseX, float.MinValue, float.MaxValue);
            _cameraTarget.rotation = Quaternion.Euler(_rotationX, _rotationY, 0f);

            transform.rotation = Quaternion.Euler(0f, _rotationY, 0f);
        }

        float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360f) angle += 360f;
            if (angle > 360f) angle -= 360f;
            return Mathf.Clamp(angle, min, max);
        }

        private void Moving()
        {
            // todo: use custom lerp instead of InputManager to ensure compatibility
            float moveForward = Input.GetAxis("Vertical");
            float moveX = Input.GetAxis("Horizontal");
            float currMotionSpeed = Mathf.Clamp01(Mathf.Abs(moveForward) + Mathf.Abs(moveX));
            float currMovingSpeed = Input.GetKey(KeyCode.LeftShift) ? _runSpeed : _walkSpeed;
            _animator.SetFloat(MotionSpeedPara, currMotionSpeed);
            _animator.SetFloat(MovingSpeedPara, currMotionSpeed * currMovingSpeed);
            Vector3 direction = transform.TransformDirection(new Vector3(moveX, 0f, moveForward));
            _characterController.SimpleMove(direction * currMovingSpeed);
        }
    }
}