using System.Collections.Generic;

namespace MiProduction.BroAudio.Data
{
    public interface IAudioAsset
    {
        public BroAudioType AudioType { get; }
        public IEnumerable<IAudioLibrary> GetAllAudioLibraries();

#if UNITY_EDITOR
        public string AssetGUID { get; set; }
		public string AssetName { get; set; } 
#endif
    }
}