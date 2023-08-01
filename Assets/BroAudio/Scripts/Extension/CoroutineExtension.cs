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
    } 
}
