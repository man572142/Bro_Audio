using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MiProduction.Extension
{
    public static class CoroutineExtension
    {
        public static void SafeStopCoroutine(this MonoBehaviour source, Coroutine coroutine)
        {
            if (coroutine != null)
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
    } 
}
