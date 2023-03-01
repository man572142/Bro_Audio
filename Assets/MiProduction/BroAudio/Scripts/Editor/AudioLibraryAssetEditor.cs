using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using MiProduction.Extension;
using static MiProduction.Extension.LoopExtension;

namespace MiProduction.BroAudio.Library.Core
{
    [CustomEditor(typeof(AudioLibraryAsset<>), true)]
    public class AudioLibraryAssetEditor : Editor
    {
		private const string EmptyString = "<color=cyan>EmptyString</color>";

        private SerializedProperty _sets = null;
        private IAudioLibraryAsset _asset = null;
        

        private List<string> _currentLibraryGUIDs = null;


        private IEnumerable<AudioData> _currentAudioDatas = null;

        public bool IsUpdatingLibrary { get; private set; }
        public event Action<LibraryState,string> OnLibraryChange;
       

        public IAudioLibraryAsset Asset => _asset;

        private void OnEnable()
        {
            _sets = serializedObject.FindProperty("Sets");
            _asset = target as IAudioLibraryAsset;

            _currentAudioDatas = GetLatestAudioDatas();

            if (_currentLibraryGUIDs == null)
            {
                _currentLibraryGUIDs = GetGUIDListFromJson();
            }
            
        }


		public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            if (!IsUpdatingLibrary)
            {
                EditorGUILayout.PropertyField(_sets, new GUIContent("Sets"), true);
                serializedObject.ApplyModifiedProperties();
            }

            if(EditorGUI.EndChangeCheck())
			{
                LibraryState state = CheckLibraryState(out string dataName);
                OnLibraryChange?.Invoke(state,dataName);
            }
		}

        public void UpdateLibrary()
		{
            IsUpdatingLibrary = true;
            GenerateAndAssignID();
            GenerateEnum(_asset, _currentLibraryGUIDs);
            IsUpdatingLibrary = false;

            void GenerateAndAssignID()
            {
                List<int> usedIDList = new List<int>();
                for(int i = 0; i < _sets.arraySize;i++)
				{
                    SerializedProperty element = _sets.GetArrayElementAtIndex(i);
                    SerializedProperty elementID = element.FindPropertyRelative("ID");

                    if(elementID.intValue > 0)
					{
                        usedIDList.Add(elementID.intValue);
					}
                }


                for (int i = 0; i < _sets.arraySize; i++)
                {
                    SerializedProperty element = _sets.GetArrayElementAtIndex(i);
                    SerializedProperty elementName = element.FindPropertyRelative("Name");
                    SerializedProperty elementID = element.FindPropertyRelative("ID");

                    elementName.stringValue = elementName.stringValue.Replace(" ", string.Empty);
                    elementID.intValue = GetUniqueID(usedIDList);
                }
                serializedObject.ApplyModifiedProperties();
                // 這個時機點要調整到Compile結束
                //Log("All enums and IDs have been generated and assigned successfully!".SetColor(Color.green));
            }

            int GetUniqueID(IEnumerable<int> idList)
            {
                int id = 0;

                int min = _asset.AudioType.ToConstantID();
                int max = _asset.AudioType.ToNext().ToConstantID();

                Loop(() =>
                {
                    id = UnityEngine.Random.Range(min,max);
                    if (idList == null || !idList.Contains(id))
                    {
                        return Statement.Break;
                    }
                    return Statement.Continue;
                });
                return id;
            }
        }


		public LibraryState CheckLibraryState(out string outputName)
        {
            outputName = string.Empty;
            
            // Compare with previous
            AudioData previousData = default;
            foreach (AudioData data in _currentAudioDatas)
			{
                if (string.IsNullOrEmpty(data.Name))
				{
                    return LibraryState.HasEmptyName;
				}
                else if(data.Name.Equals(previousData.Name))
				{
                    outputName = data.Name;
                    return LibraryState.HasNameDuplicated;
				}
                else if(IsInvalidName(data.Name,out var errorCode))
				{
                    outputName = data.Name;
                    return LibraryState.HasInvalidName;
				}
            
                previousData = data;
            }

            // Compare with all
            List<string> nameList = new List<string>();
            foreach(AudioData data in _currentAudioDatas)
			{
                if(nameList.Contains(data.Name))
				{
                    outputName = data.Name;
                    return LibraryState.HasNameDuplicated;
                }
                nameList.Add(data.Name);
            }

            List<int> idList = new List<int>();
            foreach(AudioData data in _currentAudioDatas)
			{
                if(data.ID == 0 || idList.Contains(data.ID))
				{
                    return LibraryState.NeedToUpdate;
				}
                idList.Add(data.ID);
			}

            return LibraryState.Fine;
        }

        private IEnumerable<AudioData> GetLatestAudioDatas()
		{
            return _asset.AllAudioData;
        }
	}
}