using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MiProduction.Extension
{
    public static class CoroutineExtension
    {
        public static void StopIn(this Coroutine coroutine, MonoBehaviour source)
        {
            if (coroutine != null)
            {
                source.StopCoroutine(coroutine);
            }
        }

    } 
}
