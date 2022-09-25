using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library
{
    public interface IAudioLibraryIdentify
    {
        public string[] AllLibraryEnums { get; }
        public string LibraryTypeName { get; }  
    }

}