using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {
        private const string _defaultEnumsPath = "Assets/Scripts/Enums";
        private SerializedProperty _pathProperty;
        private bool _hasUnassignedEnum = false;
        private SerializedProperty _libraries;
        private IAudioLibraryIdentify _asset;
        private string _waitingString = string.Empty;

        private void OnEnable()
        {
            _libraries = serializedObject.FindProperty("Libraries");
            _asset = target as IAudioLibraryIdentify;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!_hasUnassignedEnum)
            {
                SetEnumsPath();

                if (IsEnumNeedRefresh())
                {
                    // 既然都這樣了，何不乾脆直接就Assign
                    EditorGUILayout.HelpBox("This library needs to be updated and reassigned", MessageType.Warning);
                }

                if (_asset != null && GUILayout.Button("Update and Assign Enums", GUILayout.Height(30f)))
                {
                    if (_asset.AllLibraryEnumNames == null)
                        return;

                    if (_asset.AllLibraryEnumNames.Length == 0)
                    {
                        EnumGenerator.Generate(_pathProperty.stringValue, _asset.LibraryTypeName, new string[0]);
                    }
                    else
                    {
                        EnumGenerator.Generate(_pathProperty.stringValue, _asset.LibraryTypeName, _asset.AllLibraryEnumNames);
                    }
                    _hasUnassignedEnum = true;
                    _waitingString = string.Empty;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Assigning enums" + _waitingString);
                AssignEnum();
                _waitingString += " .";
            }

            

            serializedObject.ApplyModifiedProperties();
        }

        private void SetEnumsPath()
        {
            _pathProperty = serializedObject.FindProperty("_enumsPath");
            if (string.IsNullOrWhiteSpace(_pathProperty.stringValue))
            {
                _pathProperty.stringValue = _defaultEnumsPath;
            }
            EditorGUILayout.LabelField("Enums Path");
			EditorGUILayout.BeginHorizontal();
			{
                EditorGUILayout.LabelField(_pathProperty.stringValue);
                if (GUILayout.Button("Change Enums Path", GUILayout.Width(150f)))
                {
                    string path = EditorUtility.OpenFolderPanel("Enums Path", _pathProperty.stringValue, _pathProperty.stringValue);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        _pathProperty.stringValue = path.Substring(path.IndexOf("Assets"));
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

        }

        private void AssignEnum()
        {
            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementEnumName = element.FindPropertyRelative("Name");
                SerializedProperty elementEnum = element.FindPropertyRelative(_asset.LibraryTypeName);

                // 先嘗試assign
                for (int e = 0; e < elementEnum.enumNames.Length; e++)
                {
                    elementEnum.enumValueIndex = 0;
                    if (elementEnum.enumNames[e] == elementEnumName.stringValue)
                    {
                        elementEnum.enumValueIndex = e;
                        break;
                    }
                }
                // 如果嘗試assign過後還是None，而且EnumName不是空的，代表還沒importAsset
                if (elementEnum.enumValueIndex == 0
                    && !string.IsNullOrWhiteSpace(elementEnumName.stringValue)
                    && elementEnumName.stringValue != "None")
                {
                    _hasUnassignedEnum = true;
                    return;
                }
            }
            _hasUnassignedEnum = false;
            Debug.Log("All enums have been assigned successfully!");
        }

        private bool IsEnumNeedRefresh()
        {
            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementEnumName = element.FindPropertyRelative("Name");
                SerializedProperty elementEnum = element.FindPropertyRelative(_asset.LibraryTypeName);

                if (elementEnumName.stringValue != elementEnum.enumNames[elementEnum.enumValueIndex])
				{
                    return true;
				}
            }
            return false;
        }
	}

}