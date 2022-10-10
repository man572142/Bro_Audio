using MiProduction.Scene;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiProduction.Scene
{
    public abstract class SceneConfigAsset<T> : Editor
    {
        public SceneConfig<T>[] SceneConfigs;

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
