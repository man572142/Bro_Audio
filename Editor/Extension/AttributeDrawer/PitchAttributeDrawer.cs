using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;

namespace Ami.BroAudio.Editor
{
    [CustomPropertyDrawer(typeof(Pitch))]
    public class PitchAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Float)
            {
#if UNITY_WEBGL
                property.floatValue = EditorGUI.Slider(position, label, property.floatValue, 0f, AudioConstant.MaxAudioSourcePitch);
#else
                property.floatValue = EditorGUI.Slider(position, label, property.floatValue, AudioConstant.MinAudioSourcePitch, AudioConstant.MaxAudioSourcePitch);
#endif
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    } 
}
