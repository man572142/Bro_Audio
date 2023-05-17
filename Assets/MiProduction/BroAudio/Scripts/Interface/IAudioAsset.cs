using System.Collections.Generic;

namespace MiProduction.BroAudio.Data
{
    public interface IAudioAsset
    {
        public BroAudioType AudioType { get; }
        public string AssetGUID { get; set; }
        public string AssetName { get; set; }
        public IEnumerable<IAudioLibrary> GetAllAudioLibraries();
    }
}