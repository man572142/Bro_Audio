using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections.Generic;
using static Ami.BroAudio.Editor.BroEditorUtility;
using UnityEditor;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
    public class SoundIDAdvancedDropdown : AdvancedDropdown
    {
        private const int MinimumLinesCount = 10;
        private const string None = "None";

        private Action<AudioEntity> _onSelectItem = null;

        public SoundIDAdvancedDropdown(AdvancedDropdownState state, Action<AudioEntity> onSelectItem) : base(state)
        {
            _onSelectItem = onSelectItem;
            minimumSize = new Vector2(0f, EditorGUIUtility.singleLineHeight * MinimumLinesCount);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(nameof(BroAudio));
            root.AddChild(new AdvancedDropdownItem(None));

            List<AudioEntity> entities = new List<AudioEntity>();

            GetAudioEntities(entities);

            entities.Sort((e1, e2) => e1.AudioAsset != null && e2.AudioAsset != null ? StringComparer.OrdinalIgnoreCase.Compare(e1.AudioAsset.AssetName, e2.AudioAsset.AssetName) : 0);

            AudioAsset lastAsset = null;
            AdvancedDropdownItem lastAssetItem = null;

            foreach (var entity in entities)
			{
                if (lastAsset != entity.AudioAsset || lastAssetItem == null)
                {
                    lastAsset = entity.AudioAsset;
                    lastAssetItem = new AdvancedDropdownItem(lastAsset != null ? lastAsset.AssetName : "Unknown");
                    root.AddChild(lastAssetItem);
                }

                lastAssetItem.AddChild(new SoundIDAdvancedDropdownItem(entity));
			}
			return root;
		}

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is SoundIDAdvancedDropdownItem soundIDItem)
            {
                _onSelectItem?.Invoke(soundIDItem.Entity);
            }
            else if (item.name == None)
            {
                _onSelectItem?.Invoke(null);
            }

            base.ItemSelected(item);
        }
    } 
}