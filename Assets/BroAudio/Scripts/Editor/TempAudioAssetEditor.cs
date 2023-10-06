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
		public void AddTempEntity()
		{
			ReorderableList.defaultBehaviours.DoAddButton(LibrariesList);
		}
	}
}