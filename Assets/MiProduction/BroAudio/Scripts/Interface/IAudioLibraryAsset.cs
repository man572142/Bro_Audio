using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library.Core
{
    public interface IAudioLibraryAsset
    {
        public string[] AllAudioDataNames { get; }
        public AudioType AudioType { get; }
        public string AssetGUID { get; }
    }

}