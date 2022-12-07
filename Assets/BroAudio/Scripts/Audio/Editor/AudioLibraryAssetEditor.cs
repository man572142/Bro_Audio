using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MiProduction.BroAudio.Library
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {
        private const string _defaultEnumsPath = "Assets/BroAudio/Scripts/Audio/Enums";
        private SerializedProperty _pathProperty;
        private bool _hasUnassignedID = false;
        private SerializedProperty _libraries;
        private IAudioLibraryAsset _asset;
        private string _waitingString = string.Empty;

        private List<AudioData> _currentAudioData = null;

        private AudioData[] _newEnumDatas = null;

        private void OnEnable()
        {
            _libraries = serializedObject.FindProperty("Libraries");
            _asset = target as IAudioLibraryAsset;
        }

		private void OnDisable()
		{
			if(_hasUnassignedID && EditorApplication.isCompiling)
			{
                Debug.LogError($"LibraryAsset:[{_asset.LibraryTypeName}] ID assigning is not finished yet! Please update the library again, and wait for the process to finish.");
			}
		}

		public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(_currentAudioData == null)
			{
                _currentAudioData = AudioJsonUtility.ReadJson();
            }

            if (!_hasUnassignedID)
            {
                SetEnumsPath();

                if (IsEnumNeedRefresh())
                {
                    EditorGUILayout.HelpBox("This library needs to be updated and reassigned", MessageType.Warning);
                }

                if (_asset != null && GUILayout.Button("Update and Assign Enums", GUILayout.Height(30f)))
                {
                    if (_asset.AllLibraryEnumNames == null)
                        return;

                    if (_asset.AllLibraryEnumNames.Length == 0)
                    {
                        // TODO : 用來清空資料?
                        Debug.LogError("功能尚未完成...");
                        //EnumGenerator.Generate(_pathProperty.stringValue, _asset.LibraryTypeName, new AudioData[0]);
                    }
                    else
					{
                        AudioJsonUtility.WriteJson(_asset.AssetGUID, _asset.AudioType, _asset.AllLibraryEnumNames,ref _currentAudioData);
                        _newEnumDatas = _currentAudioData.Where(x => x.AudioType == _asset.AudioType).ToArray();
                        EnumGenerator.Generate(_pathProperty.stringValue, _asset.LibraryTypeName, _newEnumDatas);
					}
					_hasUnassignedID = true;
                    _waitingString = string.Empty;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Assigning enums" + _waitingString);
                if (!EditorApplication.isCompiling && _newEnumDatas != null)
				{
                    AssignID();
                    _newEnumDatas = null;
                }
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

        private void AssignID()
        {

            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementEnumName = element.FindPropertyRelative("Name");
                SerializedProperty elementID = element.FindPropertyRelative("ID");

                elementID.intValue = GetEnumID(elementEnumName.stringValue);
            }
            _hasUnassignedID = false;
            serializedObject.ApplyModifiedProperties();
            Debug.Log("All enums have been assigned successfully!");

            int GetEnumID(string enumName)
            {
                // TODO : null check
                return _newEnumDatas.Where(x => x.Name == enumName).Select(x => x.ID).First();
            }
        }

		private bool IsEnumNeedRefresh()
        {
            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementName = element.FindPropertyRelative("Name");
                SerializedProperty elementID = element.FindPropertyRelative("ID");

                bool hasFreshData = false;

                if(HasEnumName(elementID.intValue,out string enumName))
				{
                    hasFreshData = !string.Equals(enumName, elementName.stringValue);
				}
                else
				{
                    hasFreshData =  !string.IsNullOrEmpty(elementName.stringValue);
				}
                if(hasFreshData)
				{
                    return true;
				}
            }
            return false;

            bool HasEnumName(int id, out string name)
            {
                name = string.Empty;
                if (_currentAudioData == null || _currentAudioData.Count == 0)
                {
                    return false;
                }

                name = _currentAudioData.Where(x => x.ID == id).Select(x => x.Name).FirstOrDefault();
                return !string.IsNullOrEmpty(name);
            }
        }

        
	}

}