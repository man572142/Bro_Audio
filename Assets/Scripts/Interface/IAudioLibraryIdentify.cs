using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library
{
    public interface IAudioLibraryIdentify
    {
        public string[] AllLibraryEnumNames { get; }
        public string LibraryTypeName { get; }  
    }

}