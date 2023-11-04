using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Ami.Extension.Editor
{
    public class EunmSeparatorIndexEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType != SerializedPropertyType.Enum)
            {
                base.OnGUI(position,property,label);
            }
            
            if(GUI.Button(position,label,EditorStyles.toolbarDropDown))
            {
                
            }

        }
    }
}