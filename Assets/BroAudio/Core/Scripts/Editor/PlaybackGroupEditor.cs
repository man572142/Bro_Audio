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
    [CustomEditor(typeof(PlaybackGroup), true)]
    public class PlaybackGroupEditor : UnityEditor.Editor
    {
        public struct AttributesContainer
        {
            public FieldInfo FieldInfo;
            private Button _button;
            private CustomDrawingMethod _customDrawer;
            private DerivativeProperty _derivativeProperty;
            private InspectorNameAttribute _inspectorName;

            public bool TryGetAndCache<T>(out T attribute) where T : PropertyAttribute
            {
                attribute = null;
                if (FieldInfo == null)
                {
                    return false;
                }

                attribute = typeof(T) switch 
                {
                    Type t when t == typeof(Button) => GetOrCreate(ref _button) as T,
                    Type t when t == typeof(CustomDrawingMethod) => GetOrCreate(ref _customDrawer) as T,
                    Type t when t == typeof(DerivativeProperty) => GetOrCreate(ref _derivativeProperty) as T,
                    Type t when t == typeof(InspectorNameAttribute) => GetOrCreate(ref _inspectorName) as T,
                    _ => throw new NotImplementedException(), 
                };
                return attribute != null;
            }

            private T GetOrCreate<T>(ref T value) where T : PropertyAttribute
            {
                value ??= FieldInfo.GetCustomAttribute<T>();
                return value;
            }
        }

        public const float AdditionalButtonWidth = 60f;
        public const float OverrideToggleWidth = 30f;
        public const float OverrideToggleOffsetX = 5f;

        private MultiColumnHeader _multiColumn = null;
        private BroInstructionHelper _instruction = new BroInstructionHelper();
        private Dictionary<string, AttributesContainer> _attributesDict = null;

        private void OnEnable()
        {
            InitMultiColumn();

            Type type = serializedObject.targetObject.GetType();
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fields = type.GetFields(bindingFlags);
            if(type.BaseType == typeof(DefaultPlaybackGroup))
            {
                fields = fields.Concat(type.BaseType.GetFields(bindingFlags)).ToArray();
            }

            _attributesDict = fields
                .Select(x => (x.Name, new AttributesContainer() { FieldInfo = x}))
                .ToDictionary(x => x.Name, y => y.Item2);
        }

        private void InitMultiColumn()
        {
            GUIContent overrideIcon = EditorGUIUtility.IconContent(IconConstant.WritingIcon);
            overrideIcon.tooltip = _instruction.GetText(Instruction.PlaybackGroup_Override);
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
            string lastRulePath = null;
            while (property.NextVisible(false))
            {
                Rect rowRect = EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(OverrideToggleOffsetX);

                    if (property.type.StartsWith(nameof(PlaybackGroup.Rule<int>)))
                    {
                        DrawRule(property, toggleWidth, nameWidth);
                        lastRulePath = property.propertyPath;
                    }
                    else
                    {
                        DraweNormalProperty(property, toggleWidth, nameWidth, lastRulePath);
                    }
                }
                EditorGUILayout.EndHorizontal();

                
                DrawRuleTooltip(property, rowRect);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawRule(SerializedProperty property, GUILayoutOption toggleWidth, GUILayoutOption nameWidth)
        {
            var overrideProp = property.FindPropertyRelative(PlaybackGroup.Rule<int>.NameOf.IsOverride);
            overrideProp.boolValue = EditorGUILayout.Toggle(GUIContent.none, overrideProp.boolValue, toggleWidth);

            using (new EditorGUI.DisabledScope(!overrideProp.boolValue))
            {
                _attributesDict.TryGetValue(property.name, out var attrContainer);
                EditorGUILayout.LabelField(GetDisplayName(property, attrContainer), nameWidth);
                var valueProp = property.FindPropertyRelative(nameof(PlaybackGroup.Rule<int>.Value));
                object customDrawerReturnValue = null;
                if (attrContainer.TryGetAndCache(out CustomDrawingMethod customDrawer) && customDrawer.Method != null)
                {
                    customDrawerReturnValue = customDrawer.Method.Invoke(target, new object[] { valueProp });
                }
                else
                {
                    EditorGUILayout.PropertyField(valueProp, GUIContent.none);
                }
                DrawValueButton(attrContainer, valueProp, customDrawerReturnValue);
            }
        }

        private void DraweNormalProperty(SerializedProperty property, GUILayoutOption toggleWidth, GUILayoutOption nameWidth, string lastRulePath)
        {
            bool isDisableGroup = false;
            EditorGUILayout.LabelField(GUIContent.none, toggleWidth);
            if(_attributesDict.TryGetValue(property.name, out var attrContainer) &&
                attrContainer.TryGetAndCache(out DerivativeProperty derivativeProp))
            {
                if(lastRulePath != null)
                {
                    var lastRule = serializedObject.FindProperty(lastRulePath);
                    isDisableGroup = !lastRule.FindPropertyRelative(PlaybackGroup.Rule<int>.NameOf.IsOverride).boolValue;
                }

                Rect rect = GUILayoutUtility.GetLastRect();
                using (new Handles.DrawingScope(Color.gray))
                {
                    Vector2 start = new Vector2(rect.x + 5f, rect.y -1f);

                    if (derivativeProp.IsEnd)
                    {
                        Vector2 mid = new Vector2(start.x, start.y + EditorGUIUtility.singleLineHeight * 0.5f);
                        Vector2 end = new Vector2(mid.x + 10f, mid.y);
                        Handles.DrawLine(start, mid);
                        Handles.DrawLine(mid, end);
                    }
                    else
                    {
                        Vector2 end = new Vector2(start.x, start.y + EditorGUIUtility.singleLineHeight + 2f);
                        Handles.DrawLine(start, end);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(isDisableGroup))
            {
                EditorGUILayout.LabelField(GetDisplayName(property, attrContainer), nameWidth);
                EditorGUILayout.PropertyField(property, GUIContent.none);
            }
        }

        private void DrawRuleTooltip(SerializedProperty property, Rect rect)
        {
            if (!string.IsNullOrEmpty(property.tooltip))
            {
                EditorGUI.LabelField(rect, new GUIContent(string.Empty, property.tooltip));
            }
        }

        private void DrawValueButton(AttributesContainer attrContainer, SerializedProperty valueProp, object customDrawerReturnValue)
        {
            if(attrContainer.TryGetAndCache(out Button button))
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
                            if (attrContainer.FieldInfo.FieldType.GetCustomAttribute<FlagsAttribute>() != null)
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

        private string GetDisplayName(SerializedProperty property, AttributesContainer attrContainer)
        {
            if(attrContainer.TryGetAndCache(out InspectorNameAttribute inspectorName))
            {
                return inspectorName.displayName;
            }
            return property.displayName;
        }
    }
}