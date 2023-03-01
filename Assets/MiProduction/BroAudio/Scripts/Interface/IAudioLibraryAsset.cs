using System.Collections.Generic;

namespace MiProduction.BroAudio.Library.Core
{
    public interface IAudioLibraryAsset
    {
        public IEnumerable<AudioData> AllAudioData { get; }

        public AudioType AudioType { get; }
        public string AssetGUID { get; set; }

        public string LibraryName { get; }
    }

}