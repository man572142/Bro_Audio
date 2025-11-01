using System.Collections.Generic;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
    public class IdGenerator : IUniqueIDGenerator
    {
        private bool _isInit = false;
        private IReadOnlyList<IAudioAsset> _assetList = null;
        private Dictionary<BroAudioType, int> _lastIDs = null;

        private void Init()
        {
            if(!TryGetCoreData(out var coreData))
            {
                return;
            }

            _assetList = coreData.Assets;
            _lastIDs = new Dictionary<BroAudioType, int>();
            _isInit = true;
        }

        public int GetSimpleUniqueID(BroAudioType audioType)
        {
            if (!_isInit)
            {
                Init();
            }

            if(audioType == BroAudioType.None)
            {
                return default;
            }

            if(!_lastIDs.TryGetValue(audioType, out int lastID))
            {
                foreach (IAudioAsset asset in _assetList)
                {
                    foreach (var entity in asset.GetAllAudioEntities())
                    {
                        if (Utility.GetAudioType(entity.ID) == audioType && entity.ID > lastID)
                        {
                            lastID = entity.ID;
                        }
                    }
                }
            }

            int newID = lastID == default ? audioType.GetInitialID() : lastID + 1;
            _lastIDs[audioType] = newID;
            return newID;
        }
    } 
}
