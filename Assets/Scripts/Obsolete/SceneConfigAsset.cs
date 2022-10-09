//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;

//[CreateAssetMenu(fileName = "SceneConfig" , menuName = "BroAudio/SceneConfig")]
//[CustomEditor(typeof(SceneConfigAsset))]
//public class SceneConfigAsset : Editor
//{
//    public SceneConfig[] sceneConfigs;

//    private SceneConfigType configType;
//    private SerializedProperty configProperty;
//    private void OnEnable()
//    {
        
//    }

//    public override void OnInspectorGUI()
//    {
//        configProperty = serializedObject.FindProperty("sceneConfigs");
//        if (configProperty.arraySize > 0)
//        {
//            configType = (SceneConfigType)configProperty.GetArrayElementAtIndex(0).FindPropertyRelative("ConfigType").enumValueIndex;
//        }

//        configType = (SceneConfigType)EditorGUILayout.EnumPopup("Config Type", configType);
//        if (configProperty.arraySize > 0)
//        {
            
//            for(int i = 0; i < configProperty.arraySize;i++)
//            {
//                SerializedProperty sceneConfigEnum = configProperty.GetArrayElementAtIndex(i).FindPropertyRelative("ConfigType");
//                sceneConfigEnum.enumValueIndex = (int)configType;
//            }
//        }


//        serializedObject.ApplyModifiedProperties();
//        base.OnInspectorGUI();

        
//    }

//}
