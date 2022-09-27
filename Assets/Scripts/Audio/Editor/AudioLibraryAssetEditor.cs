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

                if (_asset != null && GUILayout.Button("Generate and Assign Enums", GUILayout.Height(30f)))
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
                AssignEnum(_asset);
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
            EditorGUILayout.LabelField(_pathProperty.stringValue);
            if (GUILayout.Button("Change Enums Path"))
            {
                string path = EditorUtility.OpenFolderPanel("Enums Path", _pathProperty.stringValue, _pathProperty.stringValue);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    _pathProperty.stringValue = path.Substring(path.IndexOf("Assets"));
                }
            }
        }

        private void AssignEnum(IAudioLibraryIdentify asset)
        {
            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementEnumName = element.FindPropertyRelative("_name");
                SerializedProperty elementEnum = element.FindPropertyRelative(asset.LibraryTypeName);

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
    }

}