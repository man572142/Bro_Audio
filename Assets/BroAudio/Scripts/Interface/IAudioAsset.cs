using System.Collections.Generic;

namespace Ami.BroAudio.Data
{
    public interface IAudioAsset
    {
        BroAudioType AudioType { get; }
        IEnumerable<IEntityIdentity> GetAllAudioEntities();

#if UNITY_EDITOR
        string AssetGUID { get; }
		string AssetName { get; } 
#endif
    }
}