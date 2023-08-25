using System.Collections.Generic;

namespace Ami.BroAudio.Data
{
    public interface IAudioAsset
    {
        BroAudioType AudioType { get; }
        IEnumerable<IAudioLibrary> GetAllAudioLibraries();

#if UNITY_EDITOR
        string AssetGUID { get; set; }
		string AssetName { get; set; } 
#endif
    }
}