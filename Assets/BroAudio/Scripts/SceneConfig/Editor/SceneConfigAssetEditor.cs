using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiProduction.Scene
{
    [CustomEditor(typeof(SceneConfigAsset<>),true)]
    public class SceneConfigAssetEditor : Editor
    {
        private void Awake()
        {
            SerializedProperty sceneConfigs = serializedObject.FindProperty("SceneConfigs");
            if (sceneConfigs.isArray && sceneConfigs.arraySize == 0)
            {
                
                sceneConfigs.arraySize = SceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneConfigs.arraySize; i++)
                {
                    sceneConfigs.GetArrayElementAtIndex(i).FindPropertyRelative("Scene").stringValue = EditorBuildSettings.scenes[i].path;
                }
                serializedObject.ApplyModifiedProperties();
            }
            
        }
    }

}