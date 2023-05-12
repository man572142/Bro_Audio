using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.LoopExtension;
using static MiProduction.Extension.EditorScriptingExtension;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.AssetEditor
{
    [CustomEditor(typeof(AudioAsset<>), true)]
    public class AudioAssetEditor : Editor,IChangesTrackable
    {
        private SerializedProperty _librariesProp = null;
        private IEnumerable<IAudioEntity> _currentAudioDatas = null;
        private List<string> _lastUpdateNameList = null;

        public bool IsCommitingChanges { get; private set; }
        public IPendinUpdatesCheckable PendingUpdates { get; set; }
        public IAudioAsset Asset { get; private set; }
		public int ChangedID { get; set; }

        private string _libraryStateOutput = string.Empty;
        private LibraryState _libraryState = LibraryState.Fine;

		private void OnEnable()
		{
			_librariesProp = serializedObject.FindProperty("Libraries");
			Asset = target as IAudioAsset;

			_currentAudioDatas = Asset.GetAllAudioEntities();
			UpdateNameList();
            CheckLibrariesState();
        }

		private void UpdateNameList()
		{
			_lastUpdateNameList = _currentAudioDatas.Select(x => x.Name).ToList();
		}

		public override void OnInspectorGUI()
        {
            if (IsCommitingChanges)
            {
                return;
            }

            // TODO: ONLY DRAW IN EDITOR WINDOW
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_librariesProp, new GUIContent("Libraries"), true);
            //serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
			{
				CheckLibrariesState();
			}
		}

		private void CheckLibrariesState()
		{
			if (CompareWithPrevious() && CompareWithAll())
			{
				PendingUpdates?.CheckChanges(this);
			}
		}

		public bool IsDirty()
        {
            if(_currentAudioDatas.Count() != _lastUpdateNameList.Count)
			{
                return true;
			}

            List<int> idList = new List<int>();
            foreach (IAudioEntity data in _currentAudioDatas)
            {
                if (data.ID == 0 || idList.Contains(data.ID))
                {
                    return true;
                }
                idList.Add(data.ID);
            }

            foreach(IAudioEntity data in _currentAudioDatas)
			{
                if(!_lastUpdateNameList.Contains(data.Name))
				{
                    return true;
				}
			}
            return false;
        }

        public void DiscardChanges()
        {

            throw new NotImplementedException();
        }

        public void CommitChanges()
		{
            IsCommitingChanges = true;
            GenerateAndAssignID();
            GenerateEnum(Asset);
            UpdateNameList();
            IsCommitingChanges = false;

            void GenerateAndAssignID()
            {
                List<int> usedIDList = new List<int>();
                for(int i = 0; i < _librariesProp.arraySize;i++)
				{
                    SerializedProperty element = _librariesProp.GetArrayElementAtIndex(i);
                    SerializedProperty elementID = element.FindPropertyRelative(GetBackingFieldName(nameof(IAudioEntity.ID)));

                    if(elementID.intValue > 0)
					{
                        usedIDList.Add(elementID.intValue);
					}
                }

                for (int i = 0; i < _librariesProp.arraySize; i++)
                {
                    SerializedProperty element = _librariesProp.GetArrayElementAtIndex(i);
                    SerializedProperty elementName = element.FindPropertyRelative(GetBackingFieldName(nameof(IAudioEntity.Name)));
                    SerializedProperty elementID = element.FindPropertyRelative(GetBackingFieldName(nameof(IAudioEntity.ID)));

                    elementName.stringValue = elementName.stringValue.Replace(" ", string.Empty);
                    elementID.intValue = GetUniqueID(usedIDList);
                }
                serializedObject.ApplyModifiedProperties();
            }

            int GetUniqueID(IEnumerable<int> idList)
            {
                int id = 0;

                int min = Asset.AudioType.ToConstantID();
                int max = Asset.AudioType.ToNext().ToConstantID();

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

        public LibraryState GetLibraryState(out string output)
		{
            output = _libraryStateOutput;
            return _libraryState;
		}


		private bool CompareWithPrevious()
		{
            IAudioEntity previousData = null;
            foreach (IAudioEntity data in _currentAudioDatas)
            {
                _libraryStateOutput = data.Name;
                if (string.IsNullOrEmpty(data.Name))
                {
                    _libraryState = LibraryState.HasEmptyName;
                    return false;
                }
                else if (previousData != null && data.Name.Equals(previousData.Name))
                {
                    _libraryState = LibraryState.HasDuplicateName;
                    return false;
                }
                else if (IsInvalidName(data.Name, out var errorCode))
                {
                    _libraryState = LibraryState.HasInvalidName;
                    return false;
                }
                previousData = data;
            }
            _libraryState = LibraryState.Fine;
            _libraryStateOutput = string.Empty;
            return true;
        }

        private bool CompareWithAll()
		{
            List<string> nameList = new List<string>();
            foreach (IAudioEntity data in _currentAudioDatas)
            {
                if (nameList.Contains(data.Name))
                {
                    _libraryStateOutput = data.Name;
                    _libraryState = LibraryState.HasDuplicateName;
                    return false;
                }
                nameList.Add(data.Name);
            }
            _libraryState = LibraryState.Fine;
            _libraryStateOutput = string.Empty;
            return true;
        }

	}
}