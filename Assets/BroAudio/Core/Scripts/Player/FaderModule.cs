using Ami.Extension;
using System;
using System.Collections;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Simulating a mixer's fader
    /// </summary>
    public class Fader
    {
        private float _origin;
        private Action _onUpdate;
        private Coroutine _coroutine;

        public float Current { get; private set; }
        public float Target { get; private set; }
        public bool IsFading => !Mathf.Approximately(Current,Target);

        private MonoBehaviour _coroutineExecutor => SoundManager.Instance;

        public Fader(float value, Action onUpdate)
        {
            _onUpdate = onUpdate;
            _origin = value;
            Target = value;
            Current = value;
        }

        public void SetTarget(float value)
        {
            _origin = Current;
            Target = value;
        }

        public void Complete(float value, bool updateMixer = true)
        {
            StopCoroutine();
            Current = value;
            Target = value;
            if(updateMixer)
            {
                _onUpdate?.Invoke();
            }
        }

        public bool Update(ref float elapsedTime, float fadeTime, Ease ease)
        {
            if(fadeTime <= 0f || _origin == Target)
            {
                Current = Target;
                return false;
            }

            if(elapsedTime < fadeTime)
            {
                Current = Mathf.Lerp(_origin, Target, (elapsedTime / fadeTime).SetEase(ease));
                _onUpdate?.Invoke();

                elapsedTime += Time.deltaTime;
                return true;
            }

            Complete(Target);
            return false;
        }

        public void StopCoroutine()
        {
            _coroutineExecutor.SafeStopCoroutine(_coroutine);
        }

        public void StartCoroutineAndReassign(IEnumerator enumerator)
        {
            _coroutineExecutor.StartCoroutineAndReassign(enumerator, ref _coroutine);
        }
    }
}