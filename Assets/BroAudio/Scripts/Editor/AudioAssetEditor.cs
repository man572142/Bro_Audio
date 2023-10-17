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
		private IUniqueIDGenerator _idGenerator = new LibraryIDController();
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

		public void Init()
		{
			Asset = target as IAudioAsset;
			InitReorderableList();
		}

		public void SetData(string guid, string assetName, BroAudioType audioType)
		{
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
		}

		private void InitReorderableList()
		{
			SerializedProperty librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			if (Asset != null)
			{
				_librariesList = new ReorderableList(librariesProp.serializedObject, librariesProp,true,false,true,true)
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
				SerializedProperty newElement = librariesProp.GetArrayElementAtIndex(list.count - 1);

				ResetLibrarySerializedProperties(newElement);
				
				var idProp = newElement.FindPropertyRelative(GetBackingFieldName(nameof(AudioLibrary.ID)));
                idProp.intValue = _idGenerator.GetUniqueID(Asset.AudioType);
				newElement.serializedObject.ApplyModifiedProperties();
				Verify();
			}

			void OnRemove(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
				Verify();
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
				Init();
				LibraryManagerWindow.OpenFromAssetFile(Asset.AssetGUID);
			}
		}

		public void DrawLibraries()
		{
			_librariesList.DoLayoutList();
		}

		public void SetAudioType(BroAudioType audioType)
		{
			SerializedProperty audioTypeProp = serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AudioType)));
			audioTypeProp.enumValueIndex = audioType.GetSerializedEnumIndex();
		}

		public SerializedProperty CreateNewEntity()
		{
			ReorderableList.defaultBehaviours.DoAddButton(_librariesList);
			SerializedProperty librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			SerializedProperty newEntity = librariesProp.GetArrayElementAtIndex(_librariesList.count - 1);
			ResetLibrarySerializedProperties(newEntity);

			return newEntity;
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