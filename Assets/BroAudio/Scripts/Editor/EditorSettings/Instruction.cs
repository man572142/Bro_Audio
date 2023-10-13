namespace Ami.BroAudio.Editor
{
	public enum Instruction
	{
        // Settings
        RuntimeSettingFileMissing,
        EditorSettingFileMissing,
        AssetOutputPathPanelTtile,

		// Settings/Audio
        HaasEffectTooltip,
		TracksAndVoicesNotMatchWarning,
		AddTracksConfirmationDialog,
        AudioVoicesToolTip,
        BroVirtualToolTip,

        // Settings/Info
        Copyright,

		// Clip Editor
        ClipEditorConfirmationDialog,

        // LibraryState
        LibraryState_IsNullOrEmpty,
        LibraryState_IsDuplicated,
        LibraryState_ContainsInvalidWords,
        LibraryState_Fine,

        // Asset Naming
        AssetNaming_IsNullOrEmpty,
        AssetNaming_ContainsWhiteSpace,
        AssetNaming_IsDuplicated,
        AssetNaming_ContainsInvalidWords,
        AssetNaming_StartWithNumber,

		// Library Manager
		LibraryManager_CreateEntity,
		LibraryManager_ModifyAsset,
		LibraryManager_MultiClipsImportTitle,
		LibraryManager_MultiClipsImportDialog,
		LibraryManager_CreateAssetWithAudioType,
		LibraryManager_ChangeAssetAudioType,
        LibraryManager_NameTempAssetHint,
    }
}