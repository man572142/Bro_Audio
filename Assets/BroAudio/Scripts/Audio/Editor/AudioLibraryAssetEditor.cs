using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Library
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {
        private const string EmptyString = "<color=cyan>EmptyString</color>";
        
        //private SerializedProperty _pathProperty;
        private bool _hasUnassignedID = false;
        private SerializedProperty _libraries;
        private IAudioLibraryAsset _asset;
        private string _waitingString = string.Empty;

        private List<AudioData> _currentAudioData = null;

        //private IEnumerable<AudioData> _newEnumDatas = null;

        private void OnEnable()
        {
            _libraries = serializedObject.FindProperty("Libraries");
            _asset = target as IAudioLibraryAsset;

            //_pathProperty = serializedObject.FindProperty("_enumsPath");

            if (_currentAudioData == null)
            {
                _currentAudioData = ReadJson();
            }
        }

		private void OnDisable()
		{
			if(_hasUnassignedID && EditorApplication.isCompiling)
			{
                LogError($"LibraryAsset:[{_asset.AudioType}] ID assigning is not finished yet! Please update the library again, and wait for the process to finish.");
			}
		}


		public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!_hasUnassignedID)
            {
                //SetEnumsPath();

                if (IsLibraryNeedRefresh())
                {
                    EditorGUILayout.HelpBox("This library needs to be updated !", MessageType.Warning);
                }

                if (GUILayout.Button("Update", GUILayout.Height(30f)) && HasAudioData(out string[] allAudioDataNames))
                {
                    WriteAudioData(_asset.AssetGUID, _asset.AudioType, allAudioDataNames, out var newAudioDatas);
                    _currentAudioData = newAudioDatas;
                    _hasUnassignedID = true;
					_waitingString = string.Empty;
				}
            }
            else
            {
                EditorGUILayout.LabelField("Assigning enums" + _waitingString);
                if (!EditorApplication.isCompiling)
				{
                    AssignID();
                }
                _waitingString += " .";
            }

            serializedObject.ApplyModifiedProperties();
        }

        //private void SetEnumsPath()
        //      {
        //          //_pathProperty = serializedObject.FindProperty("_enumsPath");
        //          if (string.IsNullOrWhiteSpace(_pathProperty.stringValue))
        //          {
        //              _pathProperty.stringValue = _defaultEnumsPath;
        //          }
        //          EditorGUILayout.LabelField("Enums Path");
        //          EditorGUILayout.BeginHorizontal();
        //          {
        //              EditorGUILayout.LabelField(_pathProperty.stringValue);
        //              if (GUILayout.Button("Change Enums Path", GUILayout.Width(150f)))
        //              {
        //                  string path = EditorUtility.OpenFolderPanel("Enums Path", _pathProperty.stringValue, _pathProperty.stringValue);
        //                  if (!string.IsNullOrWhiteSpace(path))
        //                  {
        //                      _pathProperty.stringValue = path.Substring(path.IndexOf("Assets"));
        //                  }
        //              }
        //          }
        //          EditorGUILayout.EndHorizontal();
        //      }

        private bool HasAudioData(out string[] allAudioDataNames)
        {
            allAudioDataNames = _asset.AllAudioDataNames;
            return allAudioDataNames != null && allAudioDataNames.Length > 0;
        }

        private void AssignID()
        {
            for (int i = 0; i < _libraries.arraySize; i++)
            {
                SerializedProperty element = _libraries.GetArrayElementAtIndex(i);
                SerializedProperty elementName = element.FindPropertyRelative("Name");
                SerializedProperty elementID = element.FindPropertyRelative("ID");

                elementName.stringValue = elementName.stringValue.Replace(" ", string.Empty);
                elementID.intValue = GetEnumID(elementName.stringValue,i);
            }
            _hasUnassignedID = false;
            serializedObject.ApplyModifiedProperties();
            Log("<color=#00ff00ff>All enums have been generated and assigned successfully!</color>");

            int GetEnumID(string enumName,int index)
            {          
                foreach(var data in _currentAudioData)
				{
                    if(data.Name == enumName)
					{
                        return data.ID;
					}
				}

                string subject = string.IsNullOrWhiteSpace(enumName) ? EmptyString : enumName;
                LogWarning($"Can't get audio ID with: {subject}. Element {index} has been skipped");
                return -1;
            }
        }

		private bool IsLibraryNeedRefresh()
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

                foreach(var data in _currentAudioData)
				{
                    if(data.ID == id)
					{
                        name = data.Name;
                        return !string.IsNullOrEmpty(data.Name);
                    }
				}

                return false;
            }
        }

        
	}

}