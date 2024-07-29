using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using static Ami.BroAudio.Editor.BroEditorUtility;
using UnityEditor;

namespace Ami.BroAudio.Editor
{
    public class SoundIDAdvancedDropdown : AdvancedDropdown
    {
        private const int MinimumLinesCount = 10;

        private Action<int, string, ScriptableObject> _onSelectItem = null;

        public SoundIDAdvancedDropdown(AdvancedDropdownState state, Action<int, string, ScriptableObject> onSelectItem) : base(state)
        {
            _onSelectItem = onSelectItem;
            minimumSize = new Vector2(0f, EditorGUIUtility.singleLineHeight * MinimumLinesCount);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(nameof(BroAudio));

            int childCount = 0;

            if(!TryGetCoreData(out var coreData))
            {
                return null;
            }

			foreach (var asset in coreData.Assets)
			{
				if (asset != null && !string.IsNullOrEmpty(asset.AssetName) && asset.Entities.Length > 0)
				{
					AdvancedDropdownItem item = null;
					foreach (var entity in asset.GetAllAudioEntities())
					{
						item ??= new AdvancedDropdownItem(asset.AssetName);
                        item.AddChild(new SoundIDAdvancedDropdownItem(entity.Name, entity.ID, asset));
                    }

                    if(item != null)
                    {
                        root.AddChild(item);
                        childCount++;
                    }
				}
			}
			return root;
		}

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var audioItem = item as SoundIDAdvancedDropdownItem;
            if (audioItem != null)
            {
                _onSelectItem?.Invoke(audioItem.SoundID, audioItem.name, audioItem.SourceAsset);
            }

            base.ItemSelected(item);
        }
    } 
}