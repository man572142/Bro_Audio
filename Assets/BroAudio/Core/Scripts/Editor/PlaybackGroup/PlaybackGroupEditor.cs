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
            private PropertyAttribute[] _attributes;

            public IEnumerable<PropertyAttribute> GetOrCreateAttributes()
            {
                _attributes ??= FieldInfo.GetCustomAttributes<PropertyAttribute>().ToArray();
                return _attributes;
            }

            public bool TryGet<T>(out T attribute) where T : PropertyAttribute
            {
                attribute = null;
                if (TryGet((x,_) => x.GetType() == typeof(T), out var raw))
                {
                    attribute = raw as T;
                }
                return attribute != null;
            }

            public bool TryGet(Type type, out PropertyAttribute attribute)
            {
                return TryGet((x, arg) => x.GetType() == (Type)arg, out attribute, type);
            }

            private bool TryGet(Func<PropertyAttribute, object, bool> onValidate, out PropertyAttribute attribute, object arg = null)
            {
                attribute = null;
                if (FieldInfo == null)
                {
                    return false;
                }

                foreach (var attr in GetOrCreateAttributes())
                {
                    if (onValidate.Invoke(attr, arg))
                    {
                        attribute = attr;
                        return true;
                    }
                }
                return false;
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
                .Select(x => (x.Name, new AttributesContainer() { FieldInfo = x }))
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
                _attributesDict.TryGetValue(property.name, out var attrContainer);
                DrawDecoratorField(attrContainer);

                Rect rowRect = EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(OverrideToggleOffsetX);

                    if (DrawRule(property, toggleWidth, nameWidth, attrContainer))
                    {                       
                        lastRulePath = property.propertyPath;
                    }
                    else
                    {
                        DraweNormalProperty(property, toggleWidth, nameWidth, lastRulePath, attrContainer);
                    }
                }
                EditorGUILayout.EndHorizontal();

                DrawRuleTooltip(property, rowRect);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private bool DrawRule(SerializedProperty property, GUILayoutOption toggleWidth, GUILayoutOption nameWidth, AttributesContainer attrContainer)
        {
            if (!property.TryFindPropertyRelative(Rule<int>.NameOf.IsOverride, out var overrideProp))
            {
                return false;
            }
            overrideProp.boolValue = EditorGUILayout.Toggle(GUIContent.none, overrideProp.boolValue, toggleWidth);

            using (new EditorGUI.DisabledScope(!overrideProp.boolValue))
            {
                EditorGUILayout.LabelField(GetDisplayName(property, attrContainer), nameWidth);

                if(!property.TryFindPropertyRelative(nameof(Rule<int>.Value), out var valueProp))
                {
                    return false;
                }
                object customDrawerReturnValue = null;
                if (attrContainer.TryGet(out CustomDrawingMethod customDrawer) && customDrawer.Method != null)
                {
                    customDrawerReturnValue = customDrawer.Method.Invoke(target, new object[] { valueProp });
                }
                else
                {
                    PlaybackRuleValueDrawer.DrawValue(valueProp, attrContainer);
                }
                DrawValueButton(attrContainer, valueProp, customDrawerReturnValue);
            }
            return true;
        }

        private void DraweNormalProperty(SerializedProperty property, GUILayoutOption toggleWidth, GUILayoutOption nameWidth, string lastRulePath, AttributesContainer attrContainer)
        {
            bool isDisableGroup = false;
            EditorGUILayout.LabelField(GUIContent.none, toggleWidth);
            if(attrContainer.TryGet(out DerivativeProperty derivativeProp))
            {
                if(lastRulePath != null)
                {
                    var lastRule = serializedObject.FindProperty(lastRulePath);
                    isDisableGroup = !lastRule.FindPropertyRelative(Rule<int>.NameOf.IsOverride).boolValue;
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


        private static void DrawDecoratorField(AttributesContainer attrContainer)
        {
            foreach (var attribute in attrContainer.GetOrCreateAttributes())
            {
                if (attribute is HeaderAttribute header)
                {
                    EditorGUILayout.LabelField(header.header, EditorStyles.boldLabel);
                }
                else if (attribute is SpaceAttribute space)
                {
                    EditorGUILayout.Space(space.height);
                }
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
            if(attrContainer.TryGet(out ValueButton button))
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
                        default:
                            throw new NotSupportedException();
                    }
                }
            }
        }

        private string GetDisplayName(SerializedProperty property, AttributesContainer attrContainer)
        {
            if(attrContainer.TryGet(out InspectorNameAttribute inspectorName))
            {
                return inspectorName.displayName;
            }
            return property.displayName;
        }
    }
}