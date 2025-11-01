using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class SoundIDAdvancedDropdownItem : AdvancedDropdownItem
	{
		public readonly int SoundID;
		public readonly ScriptableObject SourceAsset;

		public SoundIDAdvancedDropdownItem(string name, int soundID, ScriptableObject asset) : base(name)
		{
			SoundID = soundID;
			SourceAsset = asset;
		}
	}

}