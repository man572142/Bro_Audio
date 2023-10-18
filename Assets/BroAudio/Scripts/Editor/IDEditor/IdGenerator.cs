using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Editor.BroEditorUtility;

namespace Ami.BroAudio.Editor
{
	public class IdGenerator : IUniqueIDGenerator
	{
		private bool _isInit = false;
        private Dictionary<BroAudioType, List<IAudioAsset>> _dataDict = null;

        private void Init()
		{
            _dataDict = new Dictionary<BroAudioType, List<IAudioAsset>>(); ;
            List<string> guidList = GetGUIDListFromJson();

            foreach(string guid in guidList)
			{
                string path = AssetDatabase.GUIDToAssetPath(guid);
                IAudioAsset asset = AssetDatabase.LoadAssetAtPath(path,typeof(IAudioAsset)) as IAudioAsset;

                if(asset != null)
				{
                    if (!_dataDict.TryGetValue(asset.AudioType, out var assetList))
                    {
                        assetList = new List<IAudioAsset>();
                        _dataDict[asset.AudioType] = assetList;
                    }
                    assetList.Add(asset);
                }
            }
			_isInit = true;
		}

        public int GetUniqueID(IAudioAsset requestedAsset)
        {
            if (!_isInit)
            {
                Init();
            }

            BroAudioType audioType = requestedAsset.AudioType;
            if(audioType == BroAudioType.None)
			{
                return default;
			}

            if (_dataDict.TryGetValue(audioType, out var assetList))
			{
                if (!assetList.Contains(requestedAsset))
                {
                    assetList.Add(requestedAsset);
                }

                int lastID = default;
                foreach(IAudioAsset asset in assetList)
				{
                    foreach(var entity in asset.GetAllAudioLibraries())
					{
                        if(entity.ID > lastID)
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
            else
			{
                List<IAudioAsset> newList = new List<IAudioAsset>();
                newList.Add(requestedAsset);
                _dataDict[audioType] = newList;

                return audioType.GetInitialID();
            }
        }
	} 
}
