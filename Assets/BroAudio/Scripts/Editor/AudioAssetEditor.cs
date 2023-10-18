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
using static Ami.BroAudio.Utility;
using static Ami.Extension.EditorScriptingExtension;

namespace Ami.BroAudio.Editor
{
    [CustomEditor(typeof(AudioAsset), true)]
    public class AudioAssetEditor : UnityEditor.Editor
	{
        private ReorderableList _librariesList = null;
		private IUniqueIDGenerator _idGenerator = null;
		private ValidationErrorCode _entityIssue;
		public string IssueEntityName { get; private set; }
		public Instruction CurrInstruction { get; private set; }

		public IAudioAsset Asset { get; private set; }

		public void AddEntitiesNameChangeListener()
		{
			AudioLibraryPropertyDrawer.OnEntityNameChanged += Verify;
		}

		public void RemoveEntitiesNameChangeListener()
		{
			AudioLibraryPropertyDrawer.OnEntityNameChanged -= Verify;
		}

		public void Init(IUniqueIDGenerator idGenerator)
		{
			Asset = target as IAudioAsset;
			_idGenerator = idGenerator;
			InitReorderableList();
		}

		public void SetData(string guid, string assetName, BroAudioType audioType)
		{
			string assetGUIDPropertyPath = GetFieldName(nameof(IAudioAsset.AssetGUID));
			serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

			if(audioType != BroAudioType.None)
			{
				string assetNamePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AssetName));
				serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

				string audioTypePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AudioType));
				serializedObject.FindProperty(audioTypePropertyPath).enumValueIndex = audioType.GetSerializedEnumIndex();
			}

			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		private void InitReorderableList()
		{
			if (Asset != null)
			{
				_librariesList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(AudioAsset.Libraries)), true,false,true,true)
				{
					onAddCallback = OnAdd,
					onRemoveCallback = OnRemove,
					drawElementCallback = OnDrawElement,
					elementHeightCallback = OnGetPropertyHeight,
				};
			}

			void OnAdd(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
				SerializedProperty newEntity = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
				ResetLibrarySerializedProperties(newEntity);
				AssignID(newEntity);

				Verify();
			}

			void OnRemove(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
				Verify();
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				SerializedProperty elementProp = _librariesList.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(rect, elementProp);
				if(EditorGUI.EndChangeCheck())
				{
					elementProp.serializedObject.ApplyModifiedProperties();
				}
			}

			float OnGetPropertyHeight(int index)
			{
				return EditorGUI.GetPropertyHeight(_librariesList.serializedProperty.GetArrayElementAtIndex(index));
			}
		}

		private void AssignID(SerializedProperty entityProp)
		{
			AssignID(_idGenerator.GetUniqueID(Asset), entityProp);
		}

		private void AssignID(int id, SerializedProperty entityProp)
		{
			var idProp = entityProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioLibrary.ID)));
			idProp.intValue = id;
			entityProp.serializedObject.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Open " + BroName.MenuItem_LibraryManager))
			{
				LibraryManagerWindow.OpenFromAssetFile(Asset.AssetGUID, out var idGenerator);
				Init(idGenerator);
			}
		}

		public void DrawLibraries()
		{
			_librariesList.DoLayoutList();
		}

		public void SetAudioType(BroAudioType audioType)
		{
			SerializedProperty audioTypeProp = serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AudioType)));
			bool isChanged = Asset.AudioType != audioType; 
			audioTypeProp.enumValueIndex = audioType.GetSerializedEnumIndex();
			serializedObject.ApplyModifiedProperties();

			if(isChanged)
			{
				// RegenerateID
				int id = _idGenerator.GetUniqueID(Asset);
				for (int i = 0; i < _librariesList.serializedProperty.arraySize; i++)
				{
					SerializedProperty entity = _librariesList.serializedProperty.GetArrayElementAtIndex(i);
					AssignID(id, entity);
					id++;
				}
			}
		}

		public void SetAssetName(string newName)
		{
			var asset = Asset as AudioAsset;
			string path = AssetDatabase.GetAssetPath(asset);
			AssetDatabase.RenameAsset(path, newName);

			serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AssetName))).stringValue = newName;
			serializedObject.ApplyModifiedProperties();
		}

		public SerializedProperty CreateNewEntity()
		{
			ReorderableList.defaultBehaviours.DoAddButton(_librariesList);
			SerializedProperty librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			SerializedProperty newEntity = librariesProp.GetArrayElementAtIndex(_librariesList.count - 1);
			ResetLibrarySerializedProperties(newEntity);

			AssignID(newEntity);

			return newEntity;
		}

		public void SetClipList(SerializedProperty clipListProp, int index, AudioClip clip)
		{
			clipListProp.InsertArrayElementAtIndex(index);
			SerializedProperty elementProp = clipListProp.GetArrayElementAtIndex(index);
			elementProp.FindPropertyRelative(nameof(BroAudioClip.AudioClip)).objectReferenceValue = clip;
			elementProp.FindPropertyRelative(nameof(BroAudioClip.Volume)).floatValue = AudioConstant.FullVolume;
		}

		public void Verify()
		{
			if(VerifyAsset() && VerifyEntities())
			{
				CurrInstruction = default;
			}
		}

		private bool VerifyAsset()
		{
			if (IsInvalidName(Asset.AssetName, out ValidationErrorCode code))
			{
				switch (code)
				{
					case ValidationErrorCode.IsNullOrEmpty:
						CurrInstruction = Instruction.AssetNaming_IsNullOrEmpty;
						break;
					case ValidationErrorCode.StartWithNumber:
						CurrInstruction = Instruction.AssetNaming_StartWithNumber;
						break;
					case ValidationErrorCode.ContainsInvalidWord:
						CurrInstruction = Instruction.AssetNaming_ContainsInvalidWords;
						break;
					case ValidationErrorCode.ContainsWhiteSpace:
						CurrInstruction = Instruction.AssetNaming_ContainsWhiteSpace;
						break;
				}
				return false;
			}
			else if (IsTempReservedName(Asset.AssetName))
			{
				CurrInstruction = Instruction.AssetNaming_StartWithTemp;
				return false;
			}
			else if (Asset.AudioType == BroAudioType.None)
			{
				CurrInstruction = Instruction.LibraryManager_AssetAudioTypeNotSet;
				return false;
			}
			return true;
		}

		private bool VerifyEntities()
		{
			if (!CompareWithPreviousEntity() || !CompareWithAllEntities())
			{
				switch (_entityIssue)
				{
					case ValidationErrorCode.IsNullOrEmpty:
						CurrInstruction = Instruction.EntityIssue_HasEmptyName;
						break;
					case ValidationErrorCode.IsDuplicate:
						CurrInstruction = Instruction.EntityIssue_IsDuplicated;
						break;
					case ValidationErrorCode.ContainsInvalidWord:
						CurrInstruction = Instruction.EntityIssue_ContainsInvalidWords;
						break;
				}
				return false;
			}
			return true;
		}

		private bool CompareWithPreviousEntity()
		{
			IAudioLibrary previousData = null;
			foreach (IAudioLibrary data in Asset.GetAllAudioLibraries())
			{
				IssueEntityName = data.Name;
                if (string.IsNullOrWhiteSpace(data.Name))
                {
                    _entityIssue = ValidationErrorCode.IsNullOrEmpty;
                    return false;
                }
                else if (previousData != null && data.Name.Equals(previousData.Name))
				{
					_entityIssue = ValidationErrorCode.IsDuplicate;
					return false;
				}
				else
				{
					foreach(char word in data.Name)
					{
						if(!word.IsValidWord())
						{
							_entityIssue = ValidationErrorCode.ContainsInvalidWord;
                            return false;
						}
					}
				}
                previousData = data;
            }
            _entityIssue = ValidationErrorCode.NoError;
			IssueEntityName = string.Empty;
			return true;
        }

        private bool CompareWithAllEntities()
		{
			List<string> nameList = new List<string>();
			foreach (IAudioLibrary data in Asset.GetAllAudioLibraries())
			{
				if (nameList.Contains(data.Name))
				{
					IssueEntityName = data.Name;
					_entityIssue = ValidationErrorCode.IsDuplicate;
					return false;
				}
				nameList.Add(data.Name);
			}
			_entityIssue = ValidationErrorCode.NoError;
			IssueEntityName = string.Empty;
			return true;
		}
	}
}