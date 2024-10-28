using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundGroup))]
    public class SoundGroupEditor : UnityEditor.Editor
    {
        private SerializedProperty _maxPlayableProp = null;

        private void OnEnable()
        {
            _maxPlayableProp = serializedObject.FindBackingFieldProperty(nameof(SoundGroup.MaxPlayableCount));
        }

        public override void OnInspectorGUI()
        {
            DrawMaxPlayableCount();
        }

        private void DrawMaxPlayableCount()
        {
            EditorGUILayout.BeginHorizontal();
            {
                float currentValue = _maxPlayableProp.intValue <= 0 ? float.PositiveInfinity : _maxPlayableProp.intValue;
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUILayout.FloatField(_maxPlayableProp.displayName, currentValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (newValue <= 0f || float.IsInfinity(newValue) || float.IsNaN(newValue))
                    {
                        _maxPlayableProp.intValue = -1;
                    }
                    else
                    {
                        _maxPlayableProp.intValue = newValue > currentValue ? Mathf.CeilToInt(newValue) : Mathf.FloorToInt(newValue);
                    }

                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(_maxPlayableProp.intValue <= 0))
                {
                    if (GUILayout.Button("Infinity", GUILayout.Width(70f)))
                    {
                        _maxPlayableProp.intValue = -1;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}