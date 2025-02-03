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
        private const string None = "None";

        private Action<int, string, ScriptableObject> _onSelectItem = null;

        public SoundIDAdvancedDropdown(AdvancedDropdownState state, Action<int, string, ScriptableObject> onSelectItem) : base(state)
        {
            _onSelectItem = onSelectItem;
            minimumSize = new Vector2(0f, EditorGUIUtility.singleLineHeight * MinimumLinesCount);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(nameof(BroAudio));
            root.AddChild(new AdvancedDropdownItem(None));
            if(!TryGetCoreData(out var coreData))
            {
                return null;
            }

            foreach (var asset in coreData.Assets)
			{
				if (asset != null && !string.IsNullOrEmpty(asset.AssetName) && asset.EntitiesCount > 0)
				{
					AdvancedDropdownItem item = null;
					foreach (var entity in asset.GetAllAudioEntities())
					{
						item ??= new AdvancedDropdownItem(asset.AssetName);
                        item.AddChild(new SoundIDAdvancedDropdownItem(entity.Name, entity.ID, asset as Data.AudioAsset));
                    }

                    if(item != null)
                    {
                        root.AddChild(item);
                    }
				}
			}
			return root;
		}

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is SoundIDAdvancedDropdownItem soundIDItem)
            {
                _onSelectItem?.Invoke(soundIDItem.SoundID, soundIDItem.name, soundIDItem.SourceAsset);
            }
            else if(item.name == None)
            {
                _onSelectItem?.Invoke(0, item.name, null);
            }

            base.ItemSelected(item);
        }
    } 
}