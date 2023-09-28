using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Editor.Setting
{
#if BroAudio_DevOnly
	[CreateAssetMenu(menuName = "BroAudio/Create instruction asset",fileName = Tools.BroName.InstructionFileName)]
#endif
	public class BroInstruction : ScriptableObject
	{
		public const bool CanEdit = true;

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

		[ReadOnlyTextArea(!CanEdit)]
		public string BroVirtualToolTip;

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