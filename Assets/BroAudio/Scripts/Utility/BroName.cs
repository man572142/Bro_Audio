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
		public const string MainLogoPath = EditorResourcePath + "Logo_Main";
		public const string TransparentLogoPath = EditorResourcePath + "Logo_Transparent";
		public const string EditorResourcePath = "Editor/";
		#endregion

		#region MenuItem
		public const string MenuItem_BroAudio = "Tools/BroAudio/";
		public const string MenuItem_LibraryManager = "Library Manager";
		public const string MenuItem_ClipEditor = "Clip Editor";
		public const string MenuItem_Setting = "Setting";
		#endregion

		#region Audio Mixer
		public const string MixerName = "BroAudioMixer";
		public const string MasterTrackName = "Master";
		public const string GenericTrackName = "Track";
		public const string MainTrackName = "Main"; 
		#endregion

		#region Exposed Parameters Name
		public const string SendParaNameSuffix = "_Send";
		public const string PitchParaNameSuffix = "_Pitch";

		public const string DominatorTrackName = "Effect";
		public const string LowPassExposedName = DominatorTrackName + "_LowPass";
		public const string HighPassExposedName = DominatorTrackName + "_HighPass"; 
		#endregion
	}
}