using Ami.BroAudio.Data;

namespace Ami.BroAudio.Editor
{
	public interface IUniqueIDGenerator
	{
		int GetUniqueID(IAudioAsset asset);
	}
}