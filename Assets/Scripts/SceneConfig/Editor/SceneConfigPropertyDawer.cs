using MiProduction.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using static EditorDrawingUtility;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SceneConfig<>))]
public class SceneConfigPropertyDawer : PropertyDrawer,IEditorDrawer
{
    public int LineIndex { get; set; }
    public float SingleLineSpace => EditorGUIUtility.singleLineHeight + 3f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LineIndex = 0;
        SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");
        SerializedProperty dataProperty = property.FindPropertyRelative("Data");
        
        property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(this, position), property.isExpanded, new GUIContent(GetSceneName(sceneProperty.stringValue)));
        if (property.isExpanded)
        {
            EditorGUI.PropertyField(GetRectAndIterateLine(this, position), sceneProperty, new GUIContent("Scene"));

            if(dataProperty.isArray)
            {
                string typeName = dataProperty.arrayElementType.Replace("PPtr<$", string.Empty).Replace(">", string.Empty);
                EditorGUI.PropertyField(GetRectAndIterateLine(this, position), dataProperty, new GUIContent(typeName),true);
            }
            else
            {
                EditorGUI.PropertyField(GetRectAndIterateLine(this, position), dataProperty, new GUIContent(dataProperty.type));
            }
        } 

    }


    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;
        if(property.isExpanded)
        {
            height += LineIndex * SingleLineSpace;
            SerializedProperty dataProperty = property.FindPropertyRelative("Data");
            if(dataProperty.isArray && dataProperty.isExpanded)
            {
                height += (dataProperty.arraySize + 3) * EditorGUIUtility.singleLineHeight;
            }
        }
        else
        {
            property.isExpanded = false;
            height += SingleLineSpace;
        }
        return height;
    }

    private string GetSceneName(string scenePath)
    {
        return scenePath.Substring(scenePath.LastIndexOf("/") + 1).Replace(".unity", "");
    }

    

}
