using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MiProduction.BroAudio.IDEditor
{
	public class AudioIDAdvancedDropdownItem : AdvancedDropdownItem
	{
		public readonly int AudioID;
		public readonly ScriptableObject SourceAsset;

		public AudioIDAdvancedDropdownItem(string name, int audioID, ScriptableObject asset) : base(name)
		{
			AudioID = audioID;
			SourceAsset = asset;
		}
	}

}