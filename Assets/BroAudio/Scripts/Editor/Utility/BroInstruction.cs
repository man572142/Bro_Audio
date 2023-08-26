using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Editor.Setting
{
	//[CreateAssetMenu(menuName = "BroAudio(DevOnly)/Create instruction asset",fileName = BroName.InstructionFileName)]
	public class BroInstruction : ScriptableObject
	{
		public const bool CanEdit = false;

		[Header("Settings")]

		[ReadOnlyTextArea(!CanEdit)]
		public string SettingFileMissingText;

		[ReadOnlyTextArea(!CanEdit)]
		public string AssetOutputPathPanelTtile;

		[Header("Settings/Audio")]

		[ReadOnlyTextArea(!CanEdit)]
		public string HaasEffectTooltipText;

		[ReadOnlyTextArea(!CanEdit)]
		public string TracksAndVoicesNotMatchWarning;

		[ReadOnlyTextArea(!CanEdit)]
		public string AddTracksConfirmationDialog;

		[ReadOnlyTextArea(!CanEdit)]
		public string AudioVoicesToolTip;

		[Header("Settings/Info")]

		[ReadOnlyTextArea(!CanEdit)]
		public string Copyright;

		[Header("Clip Editor")]

		[ReadOnlyTextArea(!CanEdit)]
		public string ClipEditorConfirmationDialog;

		[Header("Library State")]

        [ReadOnlyTextArea(!CanEdit)]
        public string LibraryState_IsNullOrEmpty;

        [ReadOnlyTextArea(!CanEdit)]
        public string LibraryState_IsDuplicated;

        [ReadOnlyTextArea(!CanEdit)]
        public string LibraryState_ContainsInvalidWords;

        [ReadOnlyTextArea(!CanEdit)]
        public string LibraryState_Fine;

        [Header("Asset Naming")]

        [ReadOnlyTextArea(!CanEdit)]
        public string AssetNaming_IsNullOrEmpty;

        [ReadOnlyTextArea(!CanEdit)]
        public string AssetNaming_ContainsWhiteSpace;

        [ReadOnlyTextArea(!CanEdit)]
        public string AssetNaming_IsDuplicated;

        [ReadOnlyTextArea(!CanEdit)]
        public string AssetNaming_ContainsInvalidWords;

        [ReadOnlyTextArea(!CanEdit)]
        public string AssetNaming_StartWithNumber;
    }

}