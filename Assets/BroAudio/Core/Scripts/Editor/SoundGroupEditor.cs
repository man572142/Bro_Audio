using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.Extension;
using System;
using UnityEditor.IMGUI.Controls;
using static Ami.BroAudio.SoundGroup;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(SoundGroup))]
    public class SoundGroupEditor : UnityEditor.Editor
    {
        public const float AdditionalButtonWidth = 60f;
        public const float OverrideToggleWidth = 30f;
        public const float OverrideToggleOffsetX = 5f;

        private SerializedProperty _optionsProp, _maxPlayableProp, _combTimeProp;
        private GUIContent _toggleGUIContent, _combTimeGUIContent;

        private MultiColumnHeader _multiColumn = null;
        private BroInstructionHelper _instruction = new BroInstructionHelper();

        private void OnEnable()
        {
            InitGUIContents();
            InitMultiColumn();

            _optionsProp = FindBackingProperty(nameof(SoundGroup.OverrideOptions));
            _maxPlayableProp = FindBackingProperty(nameof(SoundGroup.MaxPlayableCount));
            _combTimeProp = FindBackingProperty(nameof(SoundGroup.CombFilteringTime));

            SerializedProperty FindBackingProperty(string path)
            {
                return serializedObject.FindBackingFieldProperty(path);
            }
        }

        private void InitGUIContents()
        {
            _toggleGUIContent = new GUIContent("", _instruction.GetText(Instruction.SoundGroup_Override));
            _combTimeGUIContent = new GUIContent(BroName.CombFilteringTimeName, _instruction.GetText(Instruction.CombFilteringTooltip));

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
                headerContent = new GUIContent("Name"),
                width = 80f,
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

            Rect columnRect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight);           
            _multiColumn.OnGUI(columnRect,0f);

            var toggleWidth = GUILayout.Width(_multiColumn.GetColumnRect(0).width - OverrideToggleOffsetX);
            var nameWidth = GUILayout.Width(_multiColumn.GetColumnRect(1).width);

            DrawField(OverrideOption.MaxPlayableCount, DrawMaxPlayableCount);
            DrawField(OverrideOption.CombFilteringTime, DrawCombFilteringSettings);

            serializedObject.ApplyModifiedProperties();

            void DrawField(OverrideOption option, Action<GUILayoutOption> onDrawContent)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(OverrideToggleOffsetX);
                    var flags = (OverrideOption)_optionsProp.GetEnumFlag();
                    EditorGUI.BeginChangeCheck();
                    bool isOn = EditorGUILayout.Toggle(_toggleGUIContent, flags.Contains(option), toggleWidth);
                    Rect toggleRect = GUILayoutUtility.GetLastRect();
                    EditorGUI.LabelField(toggleRect,_toggleGUIContent);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (isOn)
                        {
                            flags |= option;
                        }
                        else
                        {
                            flags &= ~option;
                        }
                        _optionsProp.SetEnumFlag((int)flags);
                    }

                    EditorGUI.BeginDisabledGroup(!isOn);
                    {
                        onDrawContent?.Invoke(nameWidth);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCombFilteringSettings(GUILayoutOption labelWidth)
        {
            EditorGUILayout.LabelField(_combTimeGUIContent, labelWidth);
            _combTimeProp.floatValue = Mathf.Max(EditorGUILayout.FloatField(_combTimeProp.floatValue), 0f);
            EditorGUILayout.Space();

            bool isDisabled = Mathf.Approximately(_combTimeProp.floatValue, RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds);
            DrawDisableButton(isDisabled, new GUIContent("Default"), () => _combTimeProp.floatValue = 0.04f);
        }

        private void DrawMaxPlayableCount(GUILayoutOption labelWidth)
        {
            EditorGUILayout.LabelField(_maxPlayableProp.displayName, labelWidth);
            float currentValue = _maxPlayableProp.intValue <= 0 ? float.PositiveInfinity : _maxPlayableProp.intValue;
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(currentValue);
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
            bool isDisabled = _maxPlayableProp.intValue <= 0;
            DrawDisableButton(isDisabled, new GUIContent("Infinity"), () => _maxPlayableProp.intValue = -1);
        }

        private void DrawDisableButton(bool isDisabled, GUIContent content, Action onClick)
        {
            using (new EditorGUI.DisabledScope(isDisabled))
            {
                if (GUILayout.Button(content, GUILayout.Width(AdditionalButtonWidth)))
                {
                    onClick?.Invoke();
                }
            }
        }
    }
}