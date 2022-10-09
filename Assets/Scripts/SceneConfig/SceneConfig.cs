using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.Scene
{
    [System.Serializable]
    public struct SceneConfig<T>
    {
        [SceneSelector]
        public string Scene;
        public T Data;

    } 
}