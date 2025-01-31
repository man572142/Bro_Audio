using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Reflection;
using Ami.Extension;
using System.Collections.Generic;
using System.Linq;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundGroup), true)]
    public class SoundGroupEditor : UnityEditor.Editor
    {
        public const float AdditionalButtonWidth = 60f;
        public const float OverrideToggleWidth = 30f;
        public const float OverrideToggleOffsetX = 5f;

        private MultiColumnHeader _multiColumn = null;
        private BroInstructionHelper _instruction = new BroInstructionHelper();
        private Dictionary<string, FieldInfo> _fieldInfoDict = null;

        private void OnEnable()
        {
            InitMultiColumn();

            Type type = serializedObject.targetObject.GetType();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(bindingFlags);
            if(type.BaseType == typeof(DefaultSoundGroup))
            {
                fields = fields.Concat(type.BaseType.GetFields(bindingFlags)).ToArray();
            }

            _fieldInfoDict = fields
                .Where(x => x.FieldType.IsGenericType &&
                    x.FieldType.GetGenericTypeDefinition() == typeof(SoundGroup.Rule<>) &&
                    (x.GetCustomAttribute<Button>() != null || x.GetCustomAttribute<CustomEditorDrawingMethod>() != null))
                .ToDictionary(x => x.Name);
        }

        private void InitMultiColumn()
        {
            GUIContent overrideIcon = EditorGUIUtility.IconContent(IconConstant.WritingIcon);
            overrideIcon.tooltip = _instruction.GetText(Instruction.SoundGroup_Override);
            var optionColumn = new MultiColumnHeaderState.Column()
            {
                headerContent = overrideIcon,
                width = OverrideToggleWidth,
                minWidth = OverrideToggleWidth,
                maxWidth = OverrideToggleWidth,
                canSort = false,
            };

            var nameColumn = new MultiColumnHeaderState.Column()
            {
                headerContent = new GUIContent("Rule"),
                width = 60f,
                canSort = false,
            };

            var contentColumn = new MultiColumnHeaderState.Column()
            {
                headerContent = new GUIContent("Value"),
                canSort = false,
            };

            var columns = new MultiColumnHeaderState.Column[] { optionColumn, nameColumn, contentColumn };
            _multiColumn = new MultiColumnHeader(new MultiColumnHeaderState(columns));
            _multiColumn.ResizeToFit();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Rect headerRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);           
            _multiColumn.OnGUI(headerRect, 0f);

            var toggleWidth = GUILayout.Width(_multiColumn.GetColumnRect(0).width - OverrideToggleOffsetX);
            var nameWidth = GUILayout.Width(_multiColumn.GetColumnRect(1).width);

            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // Enter children and skip MonoScript field
            int index = 0;
            while (property.NextVisible(false))
            {
                DrawRule(property, toggleWidth, nameWidth);
                DrawRuleTooltip(property, headerRect, index);
                index++;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRule(SerializedProperty property, GUILayoutOption toggleWidth, GUILayoutOption nameWidth)
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Space(OverrideToggleOffsetX);

                var overrideProp = property.FindPropertyRelative(SoundGroup.Rule<int>.NameOf.IsOverride);
                overrideProp.boolValue = EditorGUILayout.Toggle(GUIContent.none, overrideProp.boolValue, toggleWidth);

                EditorGUI.BeginDisabledGroup(!overrideProp.boolValue);
                {
                    EditorGUILayout.LabelField(property.displayName, nameWidth);
                    var valueProp = property.FindPropertyRelative(nameof(SoundGroup.Rule<int>.Value));
                    object customDrawerReturnValue = null;
                    _fieldInfoDict.TryGetValue(property.name, out var fieldInfo);
                    var customDrawer = fieldInfo?.GetCustomAttribute<CustomEditorDrawingMethod>();
                    if (customDrawer != null && customDrawer.Method != null)
                    {
                        customDrawerReturnValue = customDrawer.Method.Invoke(target, new object[] { valueProp });
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(valueProp, GUIContent.none);
                    }
                    DrawValueButton(fieldInfo, valueProp, customDrawerReturnValue);
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuleTooltip(SerializedProperty property, Rect headerRect, int index)
        {
            if (!string.IsNullOrEmpty(property.tooltip))
            {
                Rect rect = new Rect(headerRect);
                rect.y += EditorGUIUtility.singleLineHeight * (index + 1);
                EditorGUI.LabelField(rect, new GUIContent(string.Empty, property.tooltip));
            }
        }

        private void DrawValueButton(FieldInfo fieldInfo, SerializedProperty valueProp, object customDrawerReturnValue)
        {
            if(fieldInfo != null && fieldInfo.GetCustomAttribute<Button>() is Button button)
            {
                bool isDisabled = customDrawerReturnValue is bool ? (bool)customDrawerReturnValue : false;
                using (new EditorGUI.DisabledScope(isDisabled))
                {
                    DrawButton();
                }
            }

            void DrawButton()
            {
                var value = button.Value;
                float width = button.ButtonWidth >= 0f ? button.ButtonWidth : AdditionalButtonWidth;
                if (GUILayout.Button(button.Label, GUILayout.Width(width)))
                {
                    switch (valueProp.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            valueProp.intValue = (int)value;
                            break;
                        case SerializedPropertyType.Boolean:
                            valueProp.boolValue = (bool)value;
                            break;
                        case SerializedPropertyType.Float:
                            valueProp.floatValue = (float)value;
                            break;
                        case SerializedPropertyType.String:
                            valueProp.stringValue = (string)value;
                            break;
                        case SerializedPropertyType.Color:
                            valueProp.colorValue = (Color)value;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            valueProp.objectReferenceValue = (UnityEngine.Object)value;
                            break;
                        case SerializedPropertyType.Enum:
                            if (fieldInfo.FieldType.GetCustomAttribute<FlagsAttribute>() != null)
                            {
                                valueProp.SetEnumFlag((int)value);
                            }
                            else
                            {
                                valueProp.enumValueIndex = (int)value;
                            }
                            break;
                        case SerializedPropertyType.Vector2:
                            valueProp.vector2Value = (Vector2)value;
                            break;
                        case SerializedPropertyType.Vector3:
                            valueProp.vector3Value = (Vector3)value;
                            break;
                        case SerializedPropertyType.Vector4:
                            valueProp.vector4Value = (Vector4)value;
                            break;
                        case SerializedPropertyType.Rect:
                            valueProp.rectValue = (Rect)value;
                            break;
                        case SerializedPropertyType.AnimationCurve:
                            valueProp.animationCurveValue = (AnimationCurve)value;
                            break;
                        case SerializedPropertyType.Bounds:
                            valueProp.boundsValue = (Bounds)value;
                            break;
                        case SerializedPropertyType.Quaternion:
                            valueProp.quaternionValue = (Quaternion)value;
                            break;
                        case SerializedPropertyType.Vector2Int:
                            valueProp.vector2IntValue = (Vector2Int)value;
                            break;
                        case SerializedPropertyType.Vector3Int:
                            valueProp.vector3IntValue = (Vector3Int)value;
                            break;
                        case SerializedPropertyType.RectInt:
                            valueProp.rectIntValue = (RectInt)value;
                            break;
                        case SerializedPropertyType.BoundsInt:
                            valueProp.boundsIntValue = (BoundsInt)value;
                            break;
                        case SerializedPropertyType.ManagedReference:
                            break;
                        case SerializedPropertyType.Hash128:
                            valueProp.hash128Value = (Hash128)value;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }
    }
}