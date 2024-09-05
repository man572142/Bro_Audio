namespace Ami.BroAudio.Editor
{
	public enum Instruction
	{
        None = 0,

        // Settings
        AssetOutputPathPanelTtile = 1,
        LogAccessRecycledWarning,
        AudioPlayerPoolSize,
        AddDominatorTrack,
        RegenerateUserData,

        // Settings/Audio
        CombFilteringTooltip = 10,
		TracksAndVoicesNotMatchWarning,
		AddTracksConfirmationDialog,
        AudioVoicesToolTip,
        BroVirtualToolTip,
        PitchShiftingToolTip,
        AudioFilterSlope,
        AlwaysPlayMusicAsBGM,

        // Settings/Info
        Copyright = 20,

		// Clip Editor
        ClipEditorConfirmationDialog = 30,

        // EntityIssue
        EntityIssue_HasEmptyName = 100,
        EntityIssue_IsDuplicated,
        EntityIssue_ContainsInvalidWords,

        // Asset Naming
        AssetNaming_IsNullOrEmpty = 200,
        AssetNaming_ContainsWhiteSpace,
        AssetNaming_IsDuplicated,
        AssetNaming_ContainsInvalidWords,
        AssetNaming_StartWithNumber,
        AssetNaming_StartWithTemp,

		// Library Manager
		LibraryManager_CreateEntity = 300,
		LibraryManager_ModifyAsset,
		LibraryManager_MultiClipsImportTitle,
		LibraryManager_MultiClipsImportDialog,
		LibraryManager_CreateAssetWithAudioType,
		LibraryManager_ChangeEntityAudioType,
        LibraryManager_NameTempAssetHint,
        LibraryManager_AssetAudioTypeNotSet,
        LibraryManager_AssetUnnamed,

        // Sound Volume
        SoundVolume_ApplyOnEnable = 400,
        SoundVolume_ResetOnDisable,
        SoundVolume_AllowBoost,
        SoundVolume_FadeTime,
        SoundVolume_EditInPlayMode,

        // Sound Source
        SoundSource_PositionMode = 450,
    }
}