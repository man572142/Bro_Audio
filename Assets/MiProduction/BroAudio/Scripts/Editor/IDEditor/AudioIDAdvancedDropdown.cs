using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using static MiProduction.BroAudio.Utility;
using UnityEditor;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio.IDEditor
{
	public class AudioIDAdvancedDropdown : AdvancedDropdown
	{
		private Action<int, string, ScriptableObject> _onSelectItem = null;

		public AudioIDAdvancedDropdown(AdvancedDropdownState state, Action<int, string, ScriptableObject> onSelectItem) : base(state)
		{
			_onSelectItem = onSelectItem;
		}

		protected override AdvancedDropdownItem BuildRoot()
		{
			var root = new AdvancedDropdownItem("BroAudio");

			List<string> guids = GetGUIDListFromJson();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath(path, typeof(IAudioAsset)) as IAudioAsset;

				if (asset != null)
				{
					var item = new AdvancedDropdownItem(asset.AssetName);
					foreach (var entity in asset.GetAllAudioEntities())
					{

						item.AddChild(new AudioIDAdvancedDropdownItem(entity.Name, entity.ID, asset as ScriptableObject));
					}
					root.AddChild(item);
				}
			}

			return root;
		}

		protected override void ItemSelected(AdvancedDropdownItem item)
		{
			var audioItem = item as AudioIDAdvancedDropdownItem;
			if (audioItem != null)
			{
				_onSelectItem?.Invoke(audioItem.AudioID, audioItem.name, audioItem.SourceAsset);
			}

			base.ItemSelected(item);
		}
	} 
}