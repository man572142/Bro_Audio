using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace MiProduction.BroAudio.Config
{
    [CustomEditor(typeof(SceneMusicConfig))]
    public class SceneMusicConfigEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SceneMusicConfig controller = target as SceneMusicConfig;

            if (controller.musicScenes != null && controller.element < controller.musicScenes.Length && GUILayout.Button($"Get All Scene in Element {controller.element}"))
            {
                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    string sceneName = SceneUtility.GetScenePathByBuildIndex(i);
                    sceneName = sceneName.Substring(sceneName.LastIndexOf("/") + 1).Replace(".unity", "");
                    if (!controller.musicScenes[controller.element].Scenes.Contains(sceneName))
                    {
                        controller.musicScenes[controller.element].Scenes.Add(sceneName);
                    }

                }

            }
        }
    } 
}