using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MiProduction.Scene
{
    [CustomPropertyDrawer(typeof(SceneSelector))]
    public class SceneSelectorPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //base.OnGUI(position, property, label);

            if (property.propertyType == SerializedPropertyType.String)
            {
                var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(property.stringValue);

                EditorGUI.BeginChangeCheck();
                var newScene = EditorGUI.ObjectField(position, "Scene", oldScene, typeof(SceneAsset), true) as SceneAsset;

                if (EditorGUI.EndChangeCheck())
                {
                    var newPath = AssetDatabase.GetAssetPath(newScene);
                    property.stringValue = newPath;
                }
            }
        }
    }

}