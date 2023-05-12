using System.Collections.Generic;

namespace MiProduction.BroAudio.Data
{
    public interface IAudioAsset
    {
        public AudioType AudioType { get; }
        public string AssetGUID { get; set; }
        public string AssetName { get; set; }
        public IEnumerable<IAudioEntity> GetAllAudioEntities();
    }
}