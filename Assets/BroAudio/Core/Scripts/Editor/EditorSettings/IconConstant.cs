using UnityEditor;
using UnityEngine;

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

		private const string AudioSpeaker = "d_SceneViewAudio"; // could be On or Off depends on the version
        private const string AudioSpeakerOn = "d_SceneViewAudio On"; // used in 2021
		private const string AudioSpeakerOff = "d_SceneViewAudio Off"; // used in 2020
		public static GUIContent GetAudioSpeakerOnIcon()
        {
			// Unity completely reversed the names of icons for "d_SceneViewAudio" from Unity 2020 to 2021, but idk which version number it was changed in 2021.
			var onIcon = EditorGUIUtility.IconContent(AudioSpeakerOn);
			bool isDefaultIconOn = onIcon == null;
			return isDefaultIconOn ? EditorGUIUtility.IconContent(AudioSpeaker) : onIcon;
		}

        // GlobalSettingWindow
        public const string AssetOutputBrowser = "FolderOpened Icon";
        public const string GUISettingTab = "GUISkin Icon";
        public const string CogIcon = "_Popup@2x";
#if UNITY_2020_2_OR_NEWER
        public const string InfoTab = "UnityEditor.InspectorWindow@2x";
        public const string AudioSettingTab = "AudioMixerController On Icon";
#else
        public const string InfoTab = "_Help";
        public const string AudioSettingTab = "AudioMixerController Icon";
#endif

        // Message Icon
        public const string InfoMessage = "console.infoicon";
        public const string WarningMessage = "console.warnicon";
        public const string ErrorMessage = "console.erroricon";
    }
}