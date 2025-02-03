using System.Collections.Generic;

namespace Ami.BroAudio.Data
{
    public interface IAudioAsset
    {
        IEnumerable<IEntityIdentity> GetAllAudioEntities();
        int EntitiesCount { get; }
        PlaybackGroup PlaybackGroup { get; }
        void LinkPlaybackGroup(PlaybackGroup upperGroup);

#if UNITY_EDITOR
        string AssetGUID { get; }
		string AssetName { get; } 
#endif
    }
}