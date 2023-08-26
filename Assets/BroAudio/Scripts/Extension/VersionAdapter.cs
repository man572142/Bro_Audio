using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ami.Extension
{
    public static class VersionAdapter
    {
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : Component
        {
#if UNITY_2019_2_OR_NEWER
            return gameObject.TryGetComponent(out component);
#else
        component = gameObject.GetComponent<T>();
        return component;
#endif
        }
    }
}