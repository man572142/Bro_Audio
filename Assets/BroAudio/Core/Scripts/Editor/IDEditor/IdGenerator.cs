using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Editor.BroEditorUtility;
using UnityEditor.VersionControl;

namespace Ami.BroAudio.Editor
{
	public class IdGenerator : IUniqueIDGenerator
	{
		private bool _isInit = false;
        //private Dictionary<BroAudioType, List<IAudioAsset>> _dataDict = null;
        private List<IAudioAsset> _assetList = null;

        private void Init()
		{
            _assetList = new List<IAudioAsset>();
            List<string> guidList = GetGUIDListFromJson();

            foreach(string guid in guidList)
			{
                string path = AssetDatabase.GUIDToAssetPath(guid);
                IAudioAsset asset = AssetDatabase.LoadAssetAtPath(path,typeof(IAudioAsset)) as IAudioAsset;
                if(asset != null)
				{              
                    _assetList.Add(asset);
                }
            }
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
