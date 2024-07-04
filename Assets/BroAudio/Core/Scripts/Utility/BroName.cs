namespace Ami.BroAudio.Tools
{
	public static class BroName
	{
		public const string TempAssetName = "Temp";
        public const string AudioPlayerPrefabName = "AudioPlayer";

		#region File Path
		public const string InstructionFileName = "BroInstruction";
		public const string RuntimeSettingFileName = "BroRuntimeSetting";
		public const string EditorSettingPath = EditorResourcePath + "BroEditorSetting";
		public const string MainLogoPath = EditorResourcePath + "Logo_Editor";
		public const string TransparentLogoPath = EditorResourcePath + "Logo_Transparent";
		public const string EditorResourcePath = "Editor/";
		public const string EditorAudioMixerName = "EditorBroAudioMixer";
		public const string EditorAudioMixerPath = EditorResourcePath + EditorAudioMixerName;
		#endregion

		#region MenuItem
		public const string MenuItem_BroAudio = "Tools/BroAudio/";
		public const string MenuItem_LibraryManager = "Library Manager";
		public const string MenuItem_ClipEditor = "Audio Clip Editor";
		public const string MenuItem_EffectEditor = "Audio Effect Editor";
		public const string MenuItem_Preferences = "Preferences";
		public const string MenuItem_Info = "Info";
		#endregion

		#region Audio Mixer
		public const string MixerName = "BroAudioMixer";
		public const string MasterTrackName = "Master";
		public const string GenericTrackName = "Track";
		public const string MainTrackName = "Main";
		public const string MainDominatedTrackName = "Main_Dominated";
		public const string EffectTrackName = "Effect";
		public const string DominatorTrackName = "Dominator";
		#endregion

		#region Exposed Parameters Name
		public const string EffectParaNameSuffix = "_Effect";
		public const string PitchParaNameSuffix = "_Pitch";
		public const string LowPassParaNameSuffix = "_LowPass";
		public const string HighPassParaNameSuffix = "_HighPass";

        public const string LowPassParaName = EffectTrackName + LowPassParaNameSuffix;
        public const string HighPassParaName = EffectTrackName + HighPassParaNameSuffix;

        public const string Dominator_LowPassParaName = MainTrackName + LowPassParaNameSuffix;
		public const string Dominator_HighPassParaName = MainTrackName + HighPassParaNameSuffix;
        #endregion
    }
}