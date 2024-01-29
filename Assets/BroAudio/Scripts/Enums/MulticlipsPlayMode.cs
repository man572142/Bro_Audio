namespace Ami.BroAudio.Data
{
	public enum MulticlipsPlayMode
	{
		Single, // Always play the first clip
		Sequence, // Play clips sequentially
		Random, // Play clips randomly with the given weight
	}
}