namespace Ami.BroAudio.Data
{
	public enum MulticlipsPlayMode
	{
		Single, // Always play the first clip
		Sequence, // Plays clip sequentially
		Random, // Plays clip randomly with the given weight
        Shuffle, // Same as random but not repeating with the previous one
    }
}