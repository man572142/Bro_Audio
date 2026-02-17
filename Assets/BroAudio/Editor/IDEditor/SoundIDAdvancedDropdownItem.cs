using System.Collections;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
	public class SoundIDAdvancedDropdownItem : AdvancedDropdownItem
	{
		public readonly AudioEntity Entity;

		public SoundIDAdvancedDropdownItem(AudioEntity entity) : base(entity.Name)
		{
            Entity = entity;
		}
	}

}