using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Ami.Extension.Editor
{
    [CustomPropertyDrawer(typeof(BeautifulEnum))]
    public class BeautifulEnumAttributeDrawer : PropertyDrawer
    {
        public struct EnumData
        {
            public string[] Names;
            public List<int> SepartorIndex;
        }

        private Dictionary<string, EnumData> _cachedEnumDatas = new Dictionary<string, EnumData>();

        public BeautifulEnum Setting => (BeautifulEnum)attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType != SerializedPropertyType.Enum)
            {
                base.OnGUI(position,property,label);
            }

            if(!_cachedEnumDatas.TryGetValue(property.propertyPath,out EnumData enumData))
            {
                // property.enumDisplayNames can't get the [InspectorName] attribute value. so i reproduce the process that Unity did in their source code.
                enumData = GetEnumData();
                _cachedEnumDatas[property.propertyPath] = enumData;
            }
            
            if (GUI.Button(position, enumData.Names[property.enumValueIndex], GetGUIStyle()))
            {
                ShowDropDownMenu(position, property, enumData);
            }
        }

        private GUIStyle GetGUIStyle()
        {
            if(Setting.FieldTextAnchor != TextAnchor.MiddleLeft)
            {
                GUIStyle style = new GUIStyle(EditorStyles.popup);
                style.alignment = Setting.FieldTextAnchor;
                return style;
            }
            return EditorStyles.popup;
        }

        private void ShowDropDownMenu(Rect position, SerializedProperty property, EnumData enumData)
        {
            GenericMenu menu = new GenericMenu();

            for (int i = 0; i < enumData.Names.Length; i++)
            {
                if (Setting.ShowSeparator && enumData.SepartorIndex.Contains(i))
                {
                    menu.AddSeparator(string.Empty);
                }
                menu.AddItem(new GUIContent(enumData.Names[i]), property.enumValueIndex == i, OnSelectEnum, i);
            }
            menu.DropDown(position);

            void OnSelectEnum(object index)
            {
                property.enumValueIndex = (int)index;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private EnumData GetEnumData()
        {
            Type enumType = fieldInfo.FieldType;
            var enumFields = enumType.GetFields();
            // There is a hidden value called "_value" in every enum, we need to skip it.  
            EnumData data = new EnumData() 
            {
                Names = new string[enumFields.Length - 1],
                SepartorIndex = new List<int>(),
            };
                    
            for (int i = 1; i < enumFields.Length; i++)
            {
                int enumIndex = i - 1;
                var attributes = enumFields[i].GetCustomAttributes(false);
                foreach(var attr in attributes)
                {
                    if(attr is InspectorNameAttribute nameAttr)
                    {
                        data.Names[enumIndex] = nameAttr.displayName;
                    }
                    else if (attr is EnumSeparator separatorAttr)
                    {
                        data.SepartorIndex.Add(enumIndex);
                    }
                }

                if(data.Names[enumIndex] == null)
                {
                    data.Names[enumIndex] = enumFields[i].Name;
                }
            }
            return data;
        }
    }
}