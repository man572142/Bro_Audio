namespace MiProduction.BroAudio
{
	[System.Flags]
	public enum BroAudioType
	{
		None = 0,

		Music = 1,
		UI = 2,
		Ambience = 4,
		SFX = 8,
		VoiceOver = 16,

		All = Music | UI | Ambience | SFX | VoiceOver,
	} 
}