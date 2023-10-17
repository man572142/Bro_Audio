using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using static Ami.BroAudio.Editor.BroEditorUtility;
using UnityEditor;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public class AudioIDAdvancedDropdown : AdvancedDropdown
	{
		private const int MinimumLinesCount = 10;

		private Action<int, string, ScriptableObject> _onSelectItem = null;

		public AudioIDAdvancedDropdown(AdvancedDropdownState state, Action<int, string, ScriptableObject> onSelectItem) : base(state)
		{
			_onSelectItem = onSelectItem;
			minimumSize = new Vector2(0f, EditorGUIUtility.singleLineHeight * MinimumLinesCount);
		}

		protected override AdvancedDropdownItem BuildRoot()
		{
			var root = new AdvancedDropdownItem(nameof(BroAudio));

			int childCount = 0;
			List<string> guids = GetGUIDListFromJson();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath(path, typeof(IAudioAsset)) as IAudioAsset;

				if (asset != null && asset.AudioType != BroAudioType.None && !string.IsNullOrEmpty(asset.AssetName))
				{
					var item = new AdvancedDropdownItem(asset.AssetName);
					foreach (var library in asset.GetAllAudioLibraries())
					{

						item.AddChild(new AudioIDAdvancedDropdownItem(library.Name, library.ID, asset as ScriptableObject));
					}
					root.AddChild(item);
					childCount++;
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