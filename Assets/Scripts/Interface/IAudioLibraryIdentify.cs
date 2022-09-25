using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Asset
{
    public interface IAudioLibraryIdentify
    {
        public IAudioLibrary[] Libraries { get; }
        public string LibraryTypeName { get; }  
    }

}