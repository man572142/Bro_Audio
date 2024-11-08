using System.Collections.Generic;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
	public class IdGenerator : IUniqueIDGenerator
	{
		private bool _isInit = false;
        private IReadOnlyList<AudioAsset> _assetList = null;

        private void Init()
		{
            if(!TryGetCoreData(out var coreData))
            {
                return;
            }

            _assetList = coreData.Assets;
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

            int lastID = default;
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

            if (lastID == default)
            {
                return audioType.GetInitialID();
            }
            else
            {
                return lastID + 1;
            }
        }
	} 
}
