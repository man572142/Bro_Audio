namespace Ami.BroAudio.Editor
{
	public interface IUniqueIDGenerator
	{
		int GetUniqueID(BroAudioType audioType);
	}
}