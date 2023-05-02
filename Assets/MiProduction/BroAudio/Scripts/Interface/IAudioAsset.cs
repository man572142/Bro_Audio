using System.Collections.Generic;

namespace MiProduction.BroAudio.Data
{
    public interface IAudioAsset
    {
        public IEnumerable<Data.AudioData> AllAudioData { get; }
        public AudioType AudioType { get; }
        public string AssetGUID { get; set; }
        public string AssetName { get; }
    }

}