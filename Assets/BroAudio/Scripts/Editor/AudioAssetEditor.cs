using System;
using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Tools;
using Ami.Extension;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(AudioAsset), true)]
    public class AudioAssetEditor : UnityEditor.Editor
	{
        protected ReorderableList LibrariesList = null;

		private string _issueData = string.Empty;
		private DataIssue _dataIssue = DataIssue.None;
		private IEnumerable<IAudioLibrary> _currentAudioDatas = null;
		private IUniqueIDGenerator _idGenerator = new LibraryIDController();

		public IAudioAsset Asset { get; private set; }

		public void Init(string guid, string assetName = null, BroAudioType audioType = default)
		{
			Asset = target as IAudioAsset;
			_currentAudioDatas = Asset.GetAllAudioLibraries();

			if (string.IsNullOrEmpty(Asset.AssetName))
			{
				string assetNamePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AssetName));
				serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

				string assetGUIDPropertyPath = GetFieldName(nameof(IAudioAsset.AssetGUID));
				serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

				string audioTypePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AudioType));
				serializedObject.FindProperty(audioTypePropertyPath).enumValueIndex = audioType.GetSerializedEnumIndex();

				serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			InitReorderableList();
			Verify();
		}



		private void InitReorderableList()
		{
			SerializedProperty librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			if (Asset != null)
			{
				LibrariesList = new ReorderableList(librariesProp.serializedObject, librariesProp,true,false,true,true)
				{
					onAddCallback = OnAdd,
					drawElementCallback = OnDrawElement,
					elementHeightCallback = OnGetPropertyHeight,
				};
			}

			void OnAdd(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
				SerializedProperty newElement = librariesProp.GetArrayElementAtIndex(list.count - 1);

				ResetLibrarySerializedProperties(newElement);
				
				var idProp = newElement.FindPropertyRelative(GetBackingFieldName(nameof(AudioLibrary.ID)));
                idProp.intValue = _idGenerator.GetUniqueID(Asset.AudioType);
				newElement.serializedObject.ApplyModifiedProperties();
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				SerializedProperty elementProp = librariesProp.GetArrayElementAtIndex(index);
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(rect, elementProp);
				if(EditorGUI.EndChangeCheck())
				{
					elementProp.serializedObject.ApplyModifiedProperties();
				}
			}

			float OnGetPropertyHeight(int index)
			{
				return EditorGUI.GetPropertyHeight(librariesProp.GetArrayElementAtIndex(index));
			}
		}

		public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Open " + BroName.MenuItem_LibraryManager))
			{
				LibraryManagerWindow.OpenFromAssetFile(Asset.AssetGUID);
			}
		}

		public void DrawLibraries()
		{
			EditorGUI.BeginChangeCheck();
			LibrariesList.DoLayoutList();
			if (EditorGUI.EndChangeCheck())
			{
				Verify();
			}
		}

		public DataIssue GetIssue(out string output)
		{
			output = _issueData;
			return _dataIssue;
		}

		public void SetAudioType(BroAudioType audioType)
		{
			SerializedProperty audioTypeProp = serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AudioType)));
			audioTypeProp.enumValueIndex = audioType.GetSerializedEnumIndex();
		}

		public SerializedProperty CreateNewEntity()
		{
			ReorderableList.defaultBehaviours.DoAddButton(LibrariesList);
			SerializedProperty librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			SerializedProperty newEntity = librariesProp.GetArrayElementAtIndex(LibrariesList.count - 1);
			ResetLibrarySerializedProperties(newEntity);

			return newEntity;
		}

		public bool Verify()
		{
			if(IsValidAsset() && CompareWithPreviousEntity() && CompareWithAllEntities())
			{
				return true;
			}
			return false;
		}

		private bool IsValidAsset()
		{
			if(string.IsNullOrWhiteSpace(Asset.AssetName))
			{
				_dataIssue = DataIssue.AssetUnnamed;
				return false;
			}



			return true;
		}

		private bool CompareWithPreviousEntity()
		{
			IAudioLibrary previousData = null;
			foreach (IAudioLibrary data in _currentAudioDatas)
			{
				_issueData = data.Name;
                if (string.IsNullOrWhiteSpace(data.Name))
                {
                    _dataIssue = DataIssue.HasEmptyEntityName;
                    return false;
                }
                else if (previousData != null && data.Name.Equals(previousData.Name))
				{
					_dataIssue = DataIssue.HasDuplicateEntityName;
					return false;
				}
				else
				{
					foreach(char word in data.Name)
					{
						if(!word.IsValidWord())
						{
							_dataIssue = DataIssue.HasInvalidEntityName;
                            return false;
						}
					}
				}
                previousData = data;
            }
            _dataIssue = DataIssue.None;
			_issueData = string.Empty;
			return true;
        }

        private bool CompareWithAllEntities()
		{
			List<string> nameList = new List<string>();
			foreach (IAudioLibrary data in _currentAudioDatas)
			{
				if (nameList.Contains(data.Name))
				{
					_issueData = data.Name;
					_dataIssue = DataIssue.HasDuplicateEntityName;
					return false;
				}
				nameList.Add(data.Name);
			}
			_dataIssue = DataIssue.None;
			_issueData = string.Empty;
			return true;
		}
	}
}