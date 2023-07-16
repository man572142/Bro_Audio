namespace MiProduction.BroAudio
{
	[System.Flags]
	public enum EffectType
	{
		None = 0,

		Volume = 1,
		LowPass = 2,
		HighPass = 4,

		All = Volume | LowPass | HighPass,
	}
}