using System;
using System.Linq;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using UnityEngine;
using Ami.BroAudio;
using static Ami.Extension.LoopExtension;

namespace Ami.BroAudio.Editor
{
	public class LibraryIDController : ILibraryIDContainer
	{
		private Dictionary<BroAudioType, List<int>> _idController = new Dictionary<BroAudioType, List<int>>();

		public void AddByAsset(IAudioAsset asset)
		{
			if (!_idController.ContainsKey(asset.AudioType))
			{
				_idController.Add(asset.AudioType, new List<int>());
			}

			_idController[asset.AudioType].AddRange(asset.GetAllAudioLibraries().Select(x => x.ID));
		}

		public int GetUniqueID(BroAudioType audioType)
		{
			int id = 0;
			int min = audioType.ToConstantID();
			int max = audioType.ToNext().ToConstantID();

			if (_idController.TryGetValue(audioType, out var idList))
			{
				Loop(() =>
				{
				// TODO: needs better uniqueID algorithm
				id = UnityEngine.Random.Range(min, max);
					if (!idList.Contains(id))
					{
						return Statement.Break;
					}
					return Statement.Continue;
				});
				_idController[audioType].Add(id);
				return id;
			}
			else
			{
				return -1;
			}
		}

		public bool RemoveID(BroAudioType audioType, int id)
		{
			if (_idController.TryGetValue(audioType, out var idList))
			{
				return idList.Remove(id);
			}
			return false;
		}

		public void Reset()
		{
			_idController.Clear();
		}
	} 
}
