using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Ami.Extension
{
    public static class CoroutineExtension
    {
        public static void SafeStopCoroutine(this MonoBehaviour source, Coroutine coroutine)
        {
            if (coroutine != null && source)
            {
                source.StopCoroutine(coroutine);
            }
        }

        public static Coroutine StartCoroutineAndReassign(this MonoBehaviour source, IEnumerator enumerator, ref Coroutine coroutine)
        {
            source.SafeStopCoroutine(coroutine);
            coroutine = source.StartCoroutine(enumerator);
            return coroutine;
        }


        public static IEnumerator GetEnumerator(this YieldInstruction instruction)
        {
            return Enumerator();

            IEnumerator Enumerator()
            {
                yield return instruction;
            }
        }

        public static void DelayInvoke(this MonoBehaviour source, Action action, float delayTime)
        {
            DelayInvoke(source, action, new WaitForSeconds(delayTime));
        }

        public static void DelayInvoke(this MonoBehaviour source, Action action, WaitForSeconds waitForSeconds)
        {
            if(waitForSeconds == null)
            {
                Debug.LogError("WaitForSeconds is null !");
                return;
            }
            source.StartCoroutine(DelayInvoke());

            IEnumerator DelayInvoke()
            {
                yield return waitForSeconds;
                action?.Invoke();
            }
        }
    } 
}
