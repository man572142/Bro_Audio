using System.Collections.Generic;
using static Ami.BroAudio.Editor.BroEditorUtility;
using static Ami.BroAudio.Utility;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Editor
{
	public class LibraryIDController : IUniqueIDGenerator
	{
		private Dictionary<BroAudioType,int> _lastIDDict = null;
		private bool _isInit = false;

		private void Init()
		{
			if(TryGetCoreData(out var coreData))
			{
				_lastIDDict = new Dictionary<BroAudioType, int>();
				List<AudioTypeLastID> idList = coreData.AudioTypeLastIDs;
				if(idList ==  null || idList.Count == 0)
                {
                    idList = CreateLastIDs();
                    WriteLastIDsToCoreData(idList);
                }

                for (int i = 0; i < idList.Count;i++)
				{
					var pair = idList[i];
					_lastIDDict.Add(pair.AudioType, pair.LastID);
				}
			}

			_isInit = true;
		}

        public int GetUniqueID(BroAudioType audioType)
        {
            if (!_isInit)
            {
                Init();
            }

            if (_lastIDDict.TryGetValue(audioType, out int lastID))
            {
                lastID++;
                if (lastID >= audioType.ToNext().GetInitialID() || lastID >= FinalIDLimit)
                {
                    // While surpassing the limit isn't entirely out of the question, it's much less likely than other potential causes.
                    LogError("Audio ID is out of range. If you encounter this error,a bug report would be appreciated.");
                    return default;
                }

                _lastIDDict[audioType] = lastID;
                WriteNewLastID(audioType, lastID);
                return lastID;
            }
            return default;
        }

		private void WriteNewLastID(BroAudioType audioType, int lastId)
		{
			RewriteCoreData((coreData) => 
			{
                for (int i = 0; i < coreData.AudioTypeLastIDs.Count; i++)
                {
                    var item = coreData.AudioTypeLastIDs[i];
                    if (item.AudioType == audioType)
                    {
                        item.LastID = lastId;
                        coreData.AudioTypeLastIDs[i] = item;
                    }
                }
            });
		}
	} 
}
