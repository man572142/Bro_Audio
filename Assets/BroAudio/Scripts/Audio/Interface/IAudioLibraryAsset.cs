using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library
{
    public interface IAudioLibraryAsset
    {
        public string[] AllAudioDataNames { get; }
        public string LibraryTypeName { get; }  
        public AudioType AudioType { get; }
        public string AssetGUID { get; }
    }

}