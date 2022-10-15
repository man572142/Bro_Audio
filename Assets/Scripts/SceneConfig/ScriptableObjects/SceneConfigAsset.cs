using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using static UnityEngine.Networking.UnityWebRequest;

namespace MiProduction.Scene
{
    public abstract class SceneConfigAsset<T> : Editor
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

        private void Awake()
        {
            if(SceneConfigs == null)
            {
                SceneConfigs = new SceneConfig<T>[SceneManager.sceneCountInBuildSettings];
                for(int i = 0; i < SceneConfigs.Length; i++)
                {
                    SceneConfigs[i].Scene = EditorBuildSettings.scenes[i].path;
                }

            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Get All Scene From BuildSetting"))
            {

            }

        }

        
    } 
}
