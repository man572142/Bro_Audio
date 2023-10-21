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
        private ReorderableList _entitiesList = null;
		private IUniqueIDGenerator _idGenerator = null;
		private ValidationErrorCode _entityIssue;
		public string IssueEntityName { get; private set; }
		public Instruction CurrInstruction { get; private set; }

		public IAudioAsset Asset { get; private set; }

		public void AddEntitiesNameChangeListener()
		{
			AudioEntityPropertyDrawer.OnEntityNameChanged += Verify;
		}

		public void RemoveEntitiesNameChangeListener()
		{
			AudioEntityPropertyDrawer.OnEntityNameChanged -= Verify;
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
				_entitiesList = new ReorderableList(serializedObject, serializedObject.FindProperty(nameof(AudioAsset.Entities)), true,false,true,true)
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
				ResetEntitySerializedProperties(newEntity);
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
				SerializedProperty elementProp = _entitiesList.serializedProperty.GetArrayElementAtIndex(index);
				EditorGUI.BeginChangeCheck();
				EditorGUI.PropertyField(rect, elementProp);
				if(EditorGUI.EndChangeCheck())
				{
					elementProp.serializedObject.ApplyModifiedProperties();
				}
			}

			float OnGetPropertyHeight(int index)
			{
				return EditorGUI.GetPropertyHeight(_entitiesList.serializedProperty.GetArrayElementAtIndex(index));
			}
		}

		private void AssignID(SerializedProperty entityProp)
		{
			AssignID(_idGenerator.GetUniqueID(Asset), entityProp);
		}

		private void AssignID(int id, SerializedProperty entityProp)
		{
			var idProp = entityProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID)));
			idProp.intValue = id;
			entityProp.serializedObject.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Open " + BroName.MenuItem_LibraryManager))
			{
				LibraryManagerWindow window = LibraryManagerWindow.ShowWindow();
				window.SelectAsset(Asset.AssetGUID);
				Init(window.IDGenerator);
			}
		}

		public void DrawEntitiesList()
		{
			_entitiesList.DoLayoutList();
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
				for (int i = 0; i < _entitiesList.serializedProperty.arraySize; i++)
				{
					SerializedProperty entity = _entitiesList.serializedProperty.GetArrayElementAtIndex(i);
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
			ReorderableList.defaultBehaviours.DoAddButton(_entitiesList);
			SerializedProperty entitiesProp = serializedObject.FindProperty(nameof(AudioAsset.Entities));
			SerializedProperty newEntity = entitiesProp.GetArrayElementAtIndex(_entitiesList.count - 1);
			ResetEntitySerializedProperties(newEntity);

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
			IEntityIdentity previousData = null;
			foreach (IEntityIdentity data in Asset.GetAllAudioEntities())
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
			foreach (IEntityIdentity data in Asset.GetAllAudioEntities())
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