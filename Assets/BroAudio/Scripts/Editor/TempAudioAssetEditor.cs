using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Ami.BroAudio.Data;
using UnityEditorInternal;

namespace Ami.BroAudio.Editor
{
	[CustomEditor(typeof(TempAudioAsset))]
	public class TempAudioAssetEditor : AudioAssetEditor
	{
		public SerializedProperty CreateTempEntity()
		{
			ReorderableList.defaultBehaviours.DoAddButton(LibrariesList);
			SerializedProperty newEntity = LibrariesList.serializedProperty.GetArrayElementAtIndex(LibrariesList.count - 1);
			BroEditorUtility.ResetLibrarySerializedProperties(newEntity);
			return newEntity;
		}
	}
}