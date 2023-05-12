using System;
using System.Linq;
using System.Collections.Generic;
using MiProduction.BroAudio.Data;
using UnityEngine;
using MiProduction.BroAudio;
using static MiProduction.Extension.LoopExtension;

namespace MiProduction.BroAudio.IDEditor
{
	public class EntityIDController : IEntityIDContainer
	{
		private Dictionary<AudioType, List<int>> _idController = new Dictionary<AudioType, List<int>>();

		public void AddByAsset(IAudioAsset asset)
		{
			if (!_idController.ContainsKey(asset.AudioType))
			{
				_idController.Add(asset.AudioType, new List<int>());
			}

			_idController[asset.AudioType].AddRange(asset.GetAllAudioEntities().Select(x => x.ID));
		}

		public int GetUniqueID(AudioType audioType)
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

		public bool RemoveID(AudioType audioType, int id)
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
