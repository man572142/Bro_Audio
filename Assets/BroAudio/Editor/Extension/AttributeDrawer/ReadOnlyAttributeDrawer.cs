using UnityEngine;
using UnityEditor;

namespace Ami.Extension
{
    [CustomPropertyDrawer(typeof(ReadOnly))]
    public class ReadOnlyAttributeDrawer : MiPropertyDrawer
    {
        public override float SingleLineSpace => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUI.PropertyField(position,property, label);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}