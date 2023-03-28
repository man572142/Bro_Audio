using System.Collections.Generic;

namespace MiProduction.BroAudio.Asset.Core
{
    public interface IAudioAsset
    {
        public IEnumerable<AudioData> AllAudioData { get; }
        public AudioType AudioType { get; }
        public string AssetGUID { get; set; }
        public string AssetName { get; }
    }

}