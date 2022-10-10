using MiProduction.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static EditorDrawingUtility;

[CustomPropertyDrawer(typeof(SceneConfig<>))]
public class SceneConfigPropertyDawer : PropertyDrawer,IEditorDrawer
{
    public int LineIndex { get; set; }
    public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");
        //base.OnGUI(position, property, new GUIContent(GetSceneName(sceneProperty.stringValue)));

        LineIndex = 0;
        
        SerializedProperty dataProperty = property.FindPropertyRelative("Data");


        property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(this, position), property.isExpanded, new GUIContent(GetSceneName(sceneProperty.stringValue)));
        if (property.isExpanded)
        {
            EditorGUI.PropertyField(GetRectAndIterateLine(this, position), sceneProperty, new GUIContent("Scene"));
        }
        if (dataProperty.isArray)
        {

        }
        else
        {
            EditorGUI.PropertyField(GetRectAndIterateLine(this, position), dataProperty, new GUIContent(dataProperty.type));
        }
        
    }


    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return property.isExpanded ? LineIndex * SingleLineSpace : SingleLineSpace;
    }

    private string GetSceneName(string scenePath)
    {
        return scenePath.Substring(scenePath.LastIndexOf("/") + 1).Replace(".unity", "");
    }

    

}
