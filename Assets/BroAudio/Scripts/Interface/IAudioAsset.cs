using System.Collections.Generic;

namespace Ami.BroAudio.Data
{
    public interface IAudioAsset
    {
        BroAudioType AudioType { get; }
        bool IsTemp { get; }
        IEnumerable<IAudioLibrary> GetAllAudioLibraries();

#if UNITY_EDITOR
        string AssetGUID { get; }
		string AssetName { get; } 
#endif
    }
}