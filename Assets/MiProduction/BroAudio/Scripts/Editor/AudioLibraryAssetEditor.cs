using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Library.Core
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {
        private const string EmptyString = "<color=cyan>EmptyString</color>";
        
        //private bool IsUpdatingLibrary = false;
        private SerializedProperty _sets = null;
        private SerializedProperty _libraryName = null;
        private IAudioLibraryAsset _asset = null;
        private string _waitingString = string.Empty;

        private List<AudioData> _currentAudioData = null;

        public bool IsInEditorWindow { get; set; } = false;
        public bool IsUpdatingLibrary { get; private set; }

        private void OnEnable()
        {
            _libraryName = serializedObject.FindProperty("LibraryName");
            _sets = serializedObject.FindProperty("Sets");
            _asset = target as IAudioLibraryAsset;

            if (_currentAudioData == null)
            {
                _currentAudioData = ReadJson();
            }
        }

		private void OnDisable()
		{
			if(IsUpdatingLibrary && EditorApplication.isCompiling)
			{
                LogError($"LibraryAsset:[{_asset.AudioType}] ID assigning is not finished yet! Please update the library again, and wait for the process to finish.");
			}
		}


		public override void OnInspectorGUI()
        {
            _libraryName.stringValue = EditorGUILayout.TextField("Library Name",_libraryName.stringValue);

            if(!IsValidName(_libraryName.stringValue,out ValidationErrorCode errorCode))
			{
				switch (errorCode)
				{
					case ValidationErrorCode.NoError:
						break;
					case ValidationErrorCode.IsNullOrEmpty:
                        EditorGUILayout.HelpBox("Please enter a Name to identify this library.\n(this will also be the enum type name in later use", MessageType.Warning);
                        break;
					case ValidationErrorCode.StartWithNumber:
                        EditorGUILayout.HelpBox("Library Name should not start with a number", MessageType.Error);
                        break;
					case ValidationErrorCode.ContainsInvalidWord:
                        EditorGUILayout.HelpBox("Library Name can only use \"Letter\",\"Number\" and \"_(Undersocre)\"", MessageType.Error);
                        break;
				}
                return;
			}

			if (!IsUpdatingLibrary)
            {
                EditorGUILayout.PropertyField(_sets, new GUIContent("Sets"), true);
                if (IsLibraryNeedRefresh() && !IsInEditorWindow)
                {
                    EditorGUILayout.HelpBox("This library needs to be updated !", MessageType.Warning);
                    if (GUILayout.Button("Update", GUILayout.Height(30f)))
					{
						UpdateLibrary();
					}
				}
            }
            else
            {
                EditorGUILayout.LabelField("Generating IDs and enums, please don't leave " + _waitingString);
                if (!EditorApplication.isCompiling)
				{
                    AssignID();
                }
                _waitingString += " .";
            }

            serializedObject.ApplyModifiedProperties();
		}

		public void UpdateLibrary()
		{
            if(HasAudioData(out string[] allAudioDataNames))
			{
                IsUpdatingLibrary = true;
                WriteAudioData(_asset.AssetGUID, _libraryName.stringValue, allAudioDataNames, _asset.AudioType, out var newAudioDatas);
                _currentAudioData = newAudioDatas;
                _waitingString = string.Empty;
            }

            bool HasAudioData(out string[] allAudioDataNames)
            {
                allAudioDataNames = _asset.AllAudioDataNames;
                return allAudioDataNames != null && allAudioDataNames.Length > 0;
            }
        }

        private void AssignID()
        {
            for (int i = 0; i < _sets.arraySize; i++)
            {
                SerializedProperty element = _sets.GetArrayElementAtIndex(i);
                SerializedProperty elementName = element.FindPropertyRelative("Name");
                SerializedProperty elementID = element.FindPropertyRelative("ID");

                elementName.stringValue = elementName.stringValue.Replace(" ", string.Empty);
                elementID.intValue = GetEnumID(elementName.stringValue,i);
            }
            IsUpdatingLibrary = false;
            serializedObject.ApplyModifiedProperties();
            Log("All enums and IDs have been generated and assigned successfully!".SetColor(Color.green));

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

		public bool IsLibraryNeedRefresh()
        {
            for (int i = 0; i < _sets.arraySize; i++)
            {
                SerializedProperty element = _sets.GetArrayElementAtIndex(i);
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