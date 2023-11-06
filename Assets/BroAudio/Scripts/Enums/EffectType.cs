namespace Ami.BroAudio
{
	[System.Flags]
	public enum EffectType
	{
		None = 0,

		Volume = 1,
		HighCut = 2,
		LowCut = 4,

		All = Volume | HighCut | LowCut,
	}
}