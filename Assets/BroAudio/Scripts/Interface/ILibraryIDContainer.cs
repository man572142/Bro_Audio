namespace Ami.BroAudio.Editor
{
	public interface ILibraryIDContainer
	{
		public int GetUniqueID(BroAudioType audioType);
		public bool RemoveID(BroAudioType audioType, int id);
	}

}