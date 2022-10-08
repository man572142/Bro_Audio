using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SceneConfig))]
public class SceneConfigPropertyDrawer : PropertyDrawer
{

    private float SingleLineSpace { get => EditorGUIUtility.singleLineHeight + 3f; }
    private int LineIndex = 0;
   
    protected Dictionary<string, (bool isFold, bool isImagesFold)> _elementState = new Dictionary<string, (bool isFold, bool isImageFold)>();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LineIndex = 0;
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");
        SerializedProperty configTypeProperty = property.FindPropertyRelative("ConfigType");


        string sceneName = GetSceneName(sceneProperty.stringValue);
        if(string.IsNullOrEmpty(sceneName))
        {
            sceneName = "NoScene";
        }

        if (!_elementState.ContainsKey(sceneName))
        {
            _elementState.Add(sceneName, (false,false));
        }
        (bool isFold, bool isImagesFold) state = _elementState[sceneName];
        state.isFold = EditorGUI.Foldout(GetRectAndIterateLine(position), state.isFold, sceneName);

        if (state.isFold)
        {
            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneProperty.stringValue);
            EditorGUI.BeginChangeCheck();
            var newScene = EditorGUI.ObjectField(GetRectAndIterateLine(position),"Scene", oldScene, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck() && newScene != null)
            {
                sceneProperty.stringValue = AssetDatabase.GetAssetPath(newScene);
                sceneName = GetSceneName(sceneProperty.stringValue);
            }

            SceneConfigType configType = (SceneConfigType)configTypeProperty.enumValueIndex;

            switch (configType)
            {
                case SceneConfigType.Music:
                    SerializedProperty musicProperty = property.FindPropertyRelative("Music");
                    Music music = (Music)musicProperty.enumValueIndex;
                    music = (Music)EditorGUI.EnumPopup(GetRectAndIterateLine(position),"Music", music);
                    musicProperty.enumValueIndex = (int)music;
                    break;
                case SceneConfigType.Image:
                    SerializedProperty imageProperty = property.FindPropertyRelative("Image");
                    EditorGUI.PropertyField(GetRectAndIterateLine(position), imageProperty, new GUIContent("Images"),imageProperty.isExpanded);
                    state.isImagesFold = imageProperty.isExpanded;
                    break;
            }
        }
        _elementState[sceneName] = state;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty imageProperty = property.FindPropertyRelative("Image");
        SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");

        float foldHeight = 0f;
        float imageHeight = 0f;
        if (_elementState.TryGetValue(GetSceneName(sceneProperty.stringValue), out var state))
        {
            foldHeight = state.isFold ?
                SingleLineSpace * (LineIndex +1) : 0f;
            imageHeight = state.isImagesFold && state.isFold ?
                 imageProperty.arraySize < 1 ? SingleLineSpace * 3 : (imageProperty.arraySize + 1) * SingleLineSpace: 0f;
        }
        return foldHeight + imageHeight + EditorGUIUtility.singleLineHeight;
    }

    protected Rect GetRectAndIterateLine(Rect position)
    {
        Rect newRect = new Rect(position.x, position.y + SingleLineSpace * LineIndex, position.width, EditorGUIUtility.singleLineHeight);
        LineIndex++;

        return newRect;
    }

    private string GetSceneName(string scenePath)
    {
        return scenePath.Substring(scenePath.LastIndexOf("/") + 1).Replace(".unity", "");
    }

}
