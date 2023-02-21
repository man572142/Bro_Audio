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
        private string _libraryName = string.Empty;

        private SerializedProperty _sets = null;
        private IAudioLibraryAsset _asset = null;
        

        private List<AudioData> _currentAudioData = null;

        private GUIStyle _libraryNameTitleStyle = null;

        
        public bool IsUpdatingLibrary { get; private set; }

        private void OnEnable()
        {
            _sets = serializedObject.FindProperty("Sets");
            _asset = target as IAudioLibraryAsset;

            if (_currentAudioData == null)
            {
                _currentAudioData = ReadJson();
            }

            _libraryNameTitleStyle = GUIStyleHelper.Instance.MiddleCenterText;
            _libraryNameTitleStyle.richText = true;
        }


		public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField(_libraryName.SetSize(25).SetColor(Color.cyan), _libraryNameTitleStyle);
            EditorGUILayout.Space(10f);

			if (!IsUpdatingLibrary)
            {
                EditorGUILayout.PropertyField(_sets, new GUIContent("Sets"), true);
    //            if (IsLibraryNeedRefresh())
    //            {
    //                EditorGUILayout.HelpBox("This library needs to be updated !", MessageType.Warning);
    //                if (GUILayout.Button("Update", GUILayout.Height(30f)))
				//	{
				//		UpdateLibrary();
				//	}
				//}
                serializedObject.ApplyModifiedProperties();
            }
		}

		public void UpdateLibrary()
		{
            if(HasAudioData(out string[] allAudioDataNames))
			{
                IsUpdatingLibrary = true;
                WriteAudioData(_asset.AssetGUID, _libraryName, allAudioDataNames, _asset.AudioType, _currentAudioData, AssignID);
                IsUpdatingLibrary = false;
            }

            bool HasAudioData(out string[] allAudioDataNames)
            {
                allAudioDataNames = _asset.AllAudioDataNames;
                return allAudioDataNames != null && allAudioDataNames.Length > 0;
            }

            void AssignID()
            {
                for (int i = 0; i < _sets.arraySize; i++)
                {
                    SerializedProperty element = _sets.GetArrayElementAtIndex(i);
                    SerializedProperty elementName = element.FindPropertyRelative("Name");
                    SerializedProperty elementID = element.FindPropertyRelative("ID");

                    elementName.stringValue = elementName.stringValue.Replace(" ", string.Empty);
                    elementID.intValue = GetEnumID(elementName.stringValue, i);
                }
                serializedObject.ApplyModifiedProperties();
                // 這個時機點要調整到Compile結束
                //Log("All enums and IDs have been generated and assigned successfully!".SetColor(Color.green));

                int GetEnumID(string enumName, int index)
                {
                    foreach (var data in _currentAudioData)
                    {
                        if (data.Name == enumName)
                        {
                            return data.ID;
                        }
                    }

                    string subject = string.IsNullOrWhiteSpace(enumName) ? EmptyString : enumName;
                    LogWarning($"Can't get audio ID with: {subject}. Element {index} has been skipped");
                    return -1;
                }
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

        public void SetLibraryName(string name)
		{
            _libraryName = name;
		}
	}
}