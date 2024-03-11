namespace Ami.BroAudio
{
	[System.Flags]
	public enum EffectType
	{
		None = 0,

		Volume = 1 << 0,
		LowPass = 1 << 1,
		HighPass = 1 << 2,
		Custom = 1 << 3,

		All = Volume | LowPass | HighPass | Custom,
	}
}