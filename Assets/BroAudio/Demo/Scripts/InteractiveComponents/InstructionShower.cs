using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Demo
{
    public class InstructionShower : InteractiveComponent
    {
        [SerializeField] CanvasGroup _instruction = null;
        [SerializeField] float _showingTime = default;
        [SerializeField] bool _lookAtPlayer = false;
        [SerializeField] SoundSource _reminderSound = null;

        private Transform _instTransform = null;
        private Coroutine _coroutine = null;

        private void Start()
        {
            _instruction.alpha = 0f;
        }

        private void Update()
        {
            if(_lookAtPlayer && InteractiveZone.IsInZone && InteractiveZone.InZoneObject)
            {
                _instTransform ??= _instruction.transform;

                Vector3 playerPos = InteractiveZone.InZoneObject.transform.position;
                Vector3 opppsitePos = _instTransform.position + (_instTransform.position - playerPos);
                _instTransform.LookAt(opppsitePos, Vector3.up);
                _instTransform.eulerAngles = new Vector3(0f, _instTransform.eulerAngles.y, _instTransform.eulerAngles.z);
            }
        }

        public override void OnInZoneChanged(bool isInZone)
        {
            if(_coroutine != null)
            {
                StopCoroutine(_coroutine);
            }
            _coroutine = StartCoroutine(AnimateCanvasGroup(isInZone));

            if(isInZone && _reminderSound)
            {
                _reminderSound.Play();
            }
        }

        private IEnumerator AnimateCanvasGroup(bool isOpening)
        {
            _instruction.alpha = isOpening ? 0f : 1f;
            float target = isOpening ? 1f : 0f;
            float sign = isOpening ? 1f : -1f;
            _showingTime = _showingTime == 0f ? 1f : _showingTime;
            while(_instruction.alpha != target)
            {
                _instruction.alpha += Time.deltaTime / _showingTime * sign;
                yield return null;
            }
        }
    }
} 