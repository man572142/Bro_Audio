using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

namespace MiProduction.Scene
{
    public abstract class SceneConfigAsset<T> : ScriptableObject
    {
        public SceneConfig<T>[] SceneConfigs;

        public bool TryGetSceneData(out T data)
        {
            try
            {
                data = (T)GetSceneData();
                return true;
            }
            catch (InvalidCastException)
            {
                data = default;
                return false;
            }
        }

        public object GetSceneData()
        {
            return SceneConfigs.Where(x => x.Scene == SceneManager.GetActiveScene().path).Select(x => x.Data).First();
        }

        
        
    } 
}
