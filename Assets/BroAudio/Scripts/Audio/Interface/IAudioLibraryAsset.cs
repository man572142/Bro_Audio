using MiProduction.BroAudio;

namespace MiProduction.BroAudio.Library
{
    public interface IAudioLibraryAsset
    {
        //public IAudioLibrary[] Libraries { get;}
        public string[] AllLibraryEnumNames { get; }
        public string LibraryTypeName { get; }  
    }

}