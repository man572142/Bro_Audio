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
        private readonly IAudioBus _bus;
        private float _origin;
        private Coroutine _coroutine;

        public float Current { get; private set; }
        public float Target { get; private set; }
        public bool IsFading => !Mathf.Approximately(Current,Target);

        private MonoBehaviour _coroutineExecutor => SoundManager.Instance;

        public Fader(float value, IAudioBus bus)
        {
            _bus = bus;
            _origin = value;
            Target = value;
            Current = value;
        }

        public void SetTarget(float value)
        {
            _origin = Current;
            Target = value;
        }

        public void Complete(float value, bool updateBus = true)
        {
            StopCoroutine();
            Current = value;
            Target = value;
            if(updateBus)
            {
                _bus.UpdateVolume();
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
                _bus.UpdateVolume();

                elapsedTime += Utility.GetDeltaTime();
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