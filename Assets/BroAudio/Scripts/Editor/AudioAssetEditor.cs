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
        private SerializedProperty _librariesProp = null;
        protected ReorderableList LibrariesList = null;

		private string _libraryStateOutput = string.Empty;
		private LibraryState _libraryState = LibraryState.Fine;
		private IEnumerable<IAudioLibrary> _currentAudioDatas = null;
		private IUniqueIDGenerator _idGenerator = null;

		public IAudioAsset Asset { get; private set; }

		private void OnEnable()
		{
			_librariesProp = serializedObject.FindProperty(nameof(AudioAsset.Libraries));
			Asset = target as IAudioAsset;
			_currentAudioDatas = Asset.GetAllAudioLibraries();

			InitReorderableList();
			CheckLibrariesState();
		}

		private void InitReorderableList()
		{
			if (Asset != null)
			{
				LibrariesList = new ReorderableList(_librariesProp.serializedObject, _librariesProp,true,false,true,true)
				{
					onAddCallback = OnAdd,
					drawElementCallback = OnDrawElement,
					elementHeightCallback = OnGetPropertyHeight,
				};
			}

			void OnAdd(ReorderableList list)
			{
				ReorderableList.defaultBehaviours.DoAddButton(list);
				SerializedProperty newElement = _librariesProp.GetArrayElementAtIndex(list.count - 1);

				ResetLibrarySerializedProperties(newElement);
				
				var idProp = newElement.FindPropertyRelative(GetBackingFieldName(nameof(AudioLibrary.ID)));
                idProp.intValue = _idGenerator.GetUniqueID(Asset.AudioType);
				newElement.serializedObject.ApplyModifiedProperties();
			}

			void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
			{
				SerializedProperty elementProp = _librariesProp.GetArrayElementAtIndex(index);
				EditorGUI.PropertyField(rect, elementProp);
				elementProp.serializedObject.ApplyModifiedProperties();
			}

			float OnGetPropertyHeight(int index)
			{
				return EditorGUI.GetPropertyHeight(_librariesProp.GetArrayElementAtIndex(index));
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
				CheckLibrariesState();
			}
		}

		public void SetIDGenerator(IUniqueIDGenerator idAccessor)
		{
			_idGenerator = idAccessor;
		}

		public LibraryState GetLibraryState(out string output)
		{
			output = _libraryStateOutput;
			return _libraryState;
		}

		public void SetAudioType(BroAudioType audioType)
		{
			SerializedProperty audioTypeProp = serializedObject.FindProperty(GetBackingFieldName(nameof(AudioAsset.AudioType)));
			audioTypeProp.enumValueIndex = audioType.GetSerializedEnumIndex();
		}

		private void CheckLibrariesState()
		{
			CompareWithPrevious();
			if(_libraryState == LibraryState.Fine)
			{
                CompareWithAll();
            }
		}

		private bool CompareWithPrevious()
		{
			IAudioLibrary previousData = null;
			foreach (IAudioLibrary data in _currentAudioDatas)
			{
				_libraryStateOutput = data.Name;
                if (string.IsNullOrWhiteSpace(data.Name))
                {
                    _libraryState = LibraryState.HasEmptyName;
                    return false;
                }
                else if (previousData != null && data.Name.Equals(previousData.Name))
				{
					_libraryState = LibraryState.HasDuplicateName;
					return false;
				}
				else
				{
					foreach(char word in data.Name)
					{
						if(!word.IsValidWord())
						{
							_libraryState = LibraryState.HasInvalidName;
                            return false;
						}
					}
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
			foreach (IAudioLibrary data in _currentAudioDatas)
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