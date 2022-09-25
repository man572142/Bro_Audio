using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Asset
{
    public interface IAudioLibraryIdentify
    {
        public string[] AllLibraryEnums { get; }
        public string LibraryTypeName { get; }  
    }

}