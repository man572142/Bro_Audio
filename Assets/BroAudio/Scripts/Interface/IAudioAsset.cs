using System.Collections.Generic;

namespace Ami.BroAudio.Data
{
    public interface IAudioAsset
    {
        BroAudioType AudioType { get; set; }
        IEnumerable<IAudioLibrary> GetAllAudioLibraries();

#if UNITY_EDITOR
        string AssetGUID { get; }
		string AssetName { get; } 
#endif
    }
}