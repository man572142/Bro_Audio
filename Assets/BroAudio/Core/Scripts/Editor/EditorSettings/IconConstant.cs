namespace Ami.BroAudio.Editor
{
    public static class IconConstant
    {
        // LibraryManager and ClipEditor
        public const string PlayButton = "PlayButton";
        public const string StopButton = "PreMatQuad";
        public const string VolumeSnapPointer = "SignalAsset Icon";
        public const string HorizontalVUMeter = "d_VUMeterTextureHorizontal";
        public const string WorksFine = "Toggle Icon";
        public const string PlaybackPosIcon = "IN foldout focus on@2x";
        public const string FadeInIcon = "AudioHighPassFilter Icon";
        public const string FadeOutIcon = "AudioLowPassFilter Icon";
        public const string BackButton = "d_tab_prev@2x";
        public const string ImportFile = "Import@2x";
        public const string AudioClip = "AudioClip Icon";
        public const string AudioGroup = "d_AudioMixerGroup Icon";
        public const string TempAssetWarning = "console.infoicon";
        public const string LoopIcon = "d_preAudioLoopOff";
#if UNITY_2023_1_OR_NEWER
        public const string LibraryManagerShortcut = "winbtn_win_rest_h"; 
#else
        public const string LibraryManagerShortcut = "d_winbtn_win_restore_a@2x";
#endif

#if UNITY_2021_2_OR_NEWER
        public const string AudioSpeakerOn = "SceneViewAudio On";
#else
        public const string AudioSpeakerOn = "SceneViewAudio";
#endif

        // PreferenceWindow
        public const string AssetOutputBrowser = "FolderOpened Icon";
        public const string GUISettingTab = "GUISkin Icon";
        public const string CogIcon = "_Popup@2x";
        public const string InfoTab = "UnityEditor.InspectorWindow@2x";
        public const string AudioSettingTab = "AudioMixerController On Icon";

        // Message Icon
        public const string InfoMessage = "console.infoicon";
        public const string WarningMessage = "console.warnicon";
        public const string ErrorMessage = "console.erroricon";

        public const string Help = "_Help";

        // Inspector
        public const string Indicator = "IN foldout act on";
    }
}