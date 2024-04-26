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
        private ReorderableList _entitiesList = null;
		private IUniqueIDGenerator _idGenerator = null;
		private ValidationErrorCode _entityIssue;
		//public string IssueEntityName { get; private set; }
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

		public void SetData(string guid, string assetName)
		{
			string assetGUIDPropertyPath = GetFieldName(nameof(IAudioAsset.AssetGUID));
			serializedObject.FindProperty(assetGUIDPropertyPath).stringValue = guid;

            string assetNamePropertyPath = GetBackingFieldName(nameof(IAudioAsset.AssetName));
            serializedObject.FindProperty(assetNamePropertyPath).stringValue = assetName;

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
					onReorderCallback = OnReorder,
				};
			}

			void OnAdd(ReorderableList list)
			{
                BroAudioType audioType = BroAudioType.None;
                if (list.count > 0)
				{
					var lastElementProp = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                    var lastElementID = lastElementProp.FindPropertyRelative(GetBackingFieldName(nameof(AudioEntity.ID))).intValue;
					audioType = Utility.GetAudioType(lastElementID); 
                }
				ReorderableList.defaultBehaviours.DoAddButton(list);
				SerializedProperty newEntity = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
				ResetEntitySerializedProperties(newEntity);
				AssignID(newEntity, audioType);
				serializedObject.ApplyModifiedProperties();
			}

			void OnRemove(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoRemoveButton(list);
				serializedObject.ApplyModifiedProperties();
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

			void OnReorder(ReorderableList list)
			{
				list.serializedProperty.serializedObject.ApplyModifiedProperties();
			}

			float OnGetPropertyHeight(int index)
			{
				return EditorGUI.GetPropertyHeight(_entitiesList.serializedProperty.GetArrayElementAtIndex(index));
			}
		}

		private void AssignID(SerializedProperty entityProp, BroAudioType audioType)
		{
			AssignID(_idGenerator.GetSimpleUniqueID(audioType), entityProp);
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
				if(Asset == null)
				{
					Asset = target as IAudioAsset;
				}
				window.SelectAsset(Asset.AssetGUID);
				Init(window.IDGenerator);
			}
		}

		public void DrawEntitiesList()
		{
			_entitiesList.DoLayoutList();
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

			AssignID(newEntity, BroAudioType.None);

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
			if(VerifyAsset())
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
			return true;
		}
	}
}