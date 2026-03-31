namespace Ami.BroAudio.Data
{
	public enum MulticlipsPlayMode
	{
		Single, // Always play the first clip
		Sequence, // Play clip sequentially
		Random, // Play clip randomly with the given weight
        Shuffle, // Same as random but not repeating with the previous one
        Velocity, // Play clip by the specified velocity 
        Chained, // Plays an intro clip once, then loops a middle clip continuously, and plays an outro clip when stopping
    }
}