using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class PlayerMoverment : MonoBehaviour
    {
        public const string MovingSpeedPara = "Speed";
        public const string MotionSpeedPara = "MotionSpeed";
        public const string JumpPara = "Jump";
        public const string GroundedPara = "Grounded";
        public const string FreeFallPara = "FreeFall";

        [SerializeField] private Animator _animator = null;
        [SerializeField] private float _walkSpeed = 3f;
        [SerializeField] private float _runSpeed = 6f;
        [SerializeField] private CharacterController _characterController = null;
        [SerializeField] private Transform _cameraTarget = null;
        [SerializeField] private float _pitchRotationSensitivity = 1f;
        [SerializeField] private float _yawRotationSensitivity = 1.5f;

        [SerializeField] private CameraCollisionDetector _camDetector = null;

        [SerializeField] private float _maxPitchAngle = 60f;
        [SerializeField] private float _minPitchAngle = -20f;

        private float _rotationX = 0f;
        private float _rotationY = 0f;
        private float _currMovingSpeed = 0f;


        // Start is called before the first frame update
        void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

		// Update is called once per frame
		void Update()
        {
            CameraRotation();

            Moving();
        }

        private void CameraRotation()
        {
            float mouseX = Input.GetAxis("Mouse X") * _yawRotationSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * _pitchRotationSensitivity;

            // this can only constraint the mouse movement b
            if (mouseX != 0f && _camDetector.HasCollisionHorizontal(mouseX))
			{
                mouseX = 0f;
			}

            if(mouseY != 0f && _camDetector.HasCollisionVertical(mouseY))
			{
                mouseY = 0f;
			}

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