namespace Ami.BroAudio
{
	[System.Flags]
	public enum BroAudioType
	{
		None = 0,

		Music = 1 << 0,
		UI = 1 << 1,
		Ambience = 1 << 2,
		SFX = 1 << 3,
		VoiceOver = 1 << 4,

		All = Music | UI | Ambience | SFX | VoiceOver,
	} 
}