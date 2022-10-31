using MiProduction.Scene;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using static EditorDrawingUtility;
using UnityEngine.UIElements;

namespace MiProduction.Scene
{
    [CustomPropertyDrawer(typeof(SceneConfig<>))]
    public class SceneConfigPropertyDrawer : PropertyDrawer, IEditorDrawer
    {
        public int DrawLineCount { get; set; }
        public float SingleLineSpace => EditorGUIUtility.singleLineHeight;
        public Rect GetRectAndIterateLine(Rect position)
        {
            return EditorDrawingUtility.GetRectAndIterateLine(this, position);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawLineCount = 0;
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");
            SerializedProperty dataProperty = property.FindPropertyRelative("Data");

            property.isExpanded = EditorGUI.Foldout(GetRectAndIterateLine(position), property.isExpanded, new GUIContent(GetSceneName(sceneProperty.stringValue)));
            if (property.isExpanded)
            {
                EditorGUI.PropertyField(GetRectAndIterateLine(position), sceneProperty, new GUIContent("Scene"));

                if (dataProperty.isArray)
                {
                    string typeName = dataProperty.arrayElementType.Replace("PPtr<$", string.Empty).Replace(">", string.Empty);
                    EditorGUI.PropertyField(GetRectAndIterateLine(position), dataProperty, new GUIContent(typeName), true);
                }
                else
                {
                    EditorGUI.PropertyField(GetRectAndIterateLine(position), dataProperty, new GUIContent(dataProperty.type));
                }
            }
            EditorGUI.EndProperty();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                SerializedProperty sceneProperty = property.FindPropertyRelative("Scene");
                SerializedProperty dataProperty = property.FindPropertyRelative("Data");
                height += EditorGUI.GetPropertyHeight(dataProperty) + EditorGUI.GetPropertyHeight(sceneProperty);
            }
            return height;
        }

        private string GetSceneName(string scenePath)
        {
            return scenePath.Substring(scenePath.LastIndexOf("/") + 1).Replace(".unity", "");
        }


    }

}