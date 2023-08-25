namespace Ami.BroAudio.Editor
{
	public interface ILibraryIDContainer
	{
		int GetUniqueID(BroAudioType audioType);
		bool RemoveID(BroAudioType audioType, int id);
	}

}