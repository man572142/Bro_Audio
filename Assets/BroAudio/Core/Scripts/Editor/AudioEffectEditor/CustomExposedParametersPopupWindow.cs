using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using UnityEditorInternal;
using System;
using static Ami.BroAudio.Tools.BroName;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Editor
{
    public class CustomExposedParametersPopupWindow : PopupWindowContent
    {
        private class EffectExposedParameter
        {
            public string Name;
            public int OriginalIndex;

            public EffectExposedParameter(string name, int index)
            {
                Name = name;
                OriginalIndex = index;
            }
        }

        public const int MaxNameLength = 64;

        private ReorderableList _reorderableList = null;
        private SerializedProperty _exposedParams = null;
        private Vector2 _scrollPos = default;
        private bool _isRename = false;
        private int _currentSelectedIndex = default;
        private GenericMenu _rightClickMenu = null;
        private int _currentRightClickIndex = default;
        private int _currentRenameIndex = default;

        public void CreateReorderableList(AudioMixer mixer)
        {
            var serializedMixer = new SerializedObject(mixer);
            _exposedParams = serializedMixer.FindProperty("m_ExposedParameters");
            List<EffectExposedParameter> filteredParams = GetFilteredExposedParameters(_exposedParams);

            _reorderableList = new ReorderableList(filteredParams, typeof(string), false, false, false, false);
            _reorderableList.drawElementCallback = DrawElement;
            _reorderableList.onSelectCallback = OnSelect;
            _reorderableList.elementHeight = 18;
            _reorderableList.headerHeight = 0;
            _reorderableList.footerHeight = 0;
            _reorderableList.showDefaultBackground = false;

            void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
            {
                if (Event.current.isMouse && Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) // Right click
                {
                    _rightClickMenu ??= CreateRightClickMenu();
                    _currentRightClickIndex = index;
                    _rightClickMenu.DropDown(rect);
                }

                if (Event.current.isMouse && Event.current.clickCount >= 2) // Double click
                {
                    _currentRenameIndex = index;
                    _isRename = true;
                }

                if (_isRename && index == _currentRenameIndex)
                {
                    EditorGUI.BeginChangeCheck();
                    string newName = EditorGUI.TextField(rect, filteredParams[index].Name);
                    if (EditorGUI.EndChangeCheck() && IsValidName(newName))
                    {
                        filteredParams[index].Name = newName;
                        ChangeExposedParameterName(filteredParams[index]);
                    }
                }
                else
                {
                    EditorGUI.LabelField(rect, filteredParams[index].Name);
                }
            }

            void OnSelect(ReorderableList list)
            {
                if (_currentSelectedIndex != list.index)
                {
                    _isRename = false;
                }
                _currentSelectedIndex = list.index;
            }
        }

        private void EnableRenameByRightClick()
        {
            _currentRenameIndex = _currentRightClickIndex;
            _isRename = true;
        }

        private GenericMenu CreateRightClickMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Unexpose"), false, DeleteExposedParameter);
            menu.AddItem(new GUIContent("Rename"), false, EnableRenameByRightClick);
            return menu;
        }

        private void DeleteExposedParameter()
        {
            var selectedParameter = _reorderableList.list[_currentRightClickIndex] as EffectExposedParameter;
            if (selectedParameter != null)
            {
                _exposedParams.DeleteArrayElementAtIndex(selectedParameter.OriginalIndex);
                _reorderableList.list.RemoveAt(_currentRightClickIndex);
                _exposedParams.serializedObject.ApplyModifiedProperties();
            }
        }

        private void ChangeExposedParameterName(EffectExposedParameter parameter)
        {
            SerializedProperty exposedParaProp = _exposedParams.GetArrayElementAtIndex(parameter.OriginalIndex);
            SerializedProperty exposedParaNameProp = exposedParaProp.FindPropertyRelative("name");
            exposedParaNameProp.stringValue = parameter.Name;
            exposedParaProp.serializedObject.ApplyModifiedProperties();
        }

        private List<EffectExposedParameter> GetFilteredExposedParameters(SerializedProperty paramsProp)
        {
            List<EffectExposedParameter> result = new List<EffectExposedParameter>();
            for (int i = 0; i < paramsProp.arraySize; i++)
            {
                SerializedProperty exposedParaProp = paramsProp.GetArrayElementAtIndex(i);
                SerializedProperty exposedParaNameProp = exposedParaProp.FindPropertyRelative("name");
                if (!IsCoreParameter(exposedParaNameProp.stringValue))
                {
                    result.Add(new EffectExposedParameter(exposedParaNameProp.stringValue, i));
                }
            }
            return result;

            bool IsCoreParameter(string paraName)
            {
                bool endWithNumber = Char.IsNumber(paraName[paraName.Length - 1]);
                bool mightBeGenericTrack = paraName.StartsWith(GenericTrackName, StringComparison.Ordinal);
                return IsGenericTrack() || IsGenericTrackEffect() || IsDominatorTrack() || IsMainTrack() || IsMasterTrack();

                bool IsGenericTrack() => endWithNumber && mightBeGenericTrack;
                bool IsGenericTrackEffect() => !endWithNumber && mightBeGenericTrack && paraName.EndsWith(EffectParaNameSuffix, StringComparison.Ordinal);
                bool IsDominatorTrack() => endWithNumber && paraName.StartsWith(DominatorTrackName, StringComparison.Ordinal);
                bool IsMainTrack() => paraName.StartsWith(MainTrackName, StringComparison.Ordinal);
                bool IsMasterTrack() => !endWithNumber && paraName.Equals(MasterTrackName);
            }
        }

        private bool IsValidName(string newName)
        {
            if (newName.Length > MaxNameLength)
            {
                Debug.LogWarning(Utility.LogTitle + $"Maximum name length of an exposed parameter is {MaxNameLength}");
                return false;
            }
            return true;
        }

        public override void OnGUI(Rect rect)
        {
            if (_reorderableList != null)
            {
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                _reorderableList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
        }
    } 
}
