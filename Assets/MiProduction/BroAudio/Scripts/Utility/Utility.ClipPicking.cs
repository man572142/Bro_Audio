using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MiProduction.BroAudio.Data;

namespace MiProduction.BroAudio
{
	public static partial class Utility
	{
		public static Dictionary<int, int> ClipsSequencer = new Dictionary<int, int>();


		public static BroAudioClip PickNewOne(this BroAudioClip[] clips, MulticlipsPlayMode playMode, int id)
		{
			if (clips == null || clips.Length <= 0)
			{
				LogError("There is no AudioClip in asset");
				return default;
			}
			else if (clips.Length == 1)
			{
				playMode = MulticlipsPlayMode.Single;
			}

			switch (playMode)
			{
				case MulticlipsPlayMode.Single:
					return clips[0];
				case MulticlipsPlayMode.Sequence:
					return clips.PickNextClip(id);
				case MulticlipsPlayMode.Random:
					return clips.PickRandomClip();
			}
			return default;
		}

		private static BroAudioClip PickNextClip(this BroAudioClip[] clips, int id)
		{
			int resultIndex = 0;
			if (ClipsSequencer.ContainsKey(id))
			{
				ClipsSequencer[id] = ClipsSequencer[id] + 1 >= clips.Length ? 0 : ClipsSequencer[id] + 1;
				resultIndex = ClipsSequencer[id];
			}
			else
			{
				ClipsSequencer.Add(id, 0);
			}
			return clips[resultIndex];
		}

		public static BroAudioClip PickRandomClip(this BroAudioClip[] clips)
		{
			int totalWeight = clips.Sum(x => x.Weight);

			// No Weight
			if (totalWeight == 0)
			{
				return clips[Random.Range(0, clips.Length)];
			}

			// Use Weight
			int targetWeight = Random.Range(0, totalWeight);
			int sum = 0;

			for (int i = 0; i < clips.Length; i++)
			{
				sum += clips[i].Weight;
				if (targetWeight < sum)
				{
					return clips[i]; ;
				}
			}
			return default;
		}

		public static void ResetSequencer(int id)
		{
			if (ClipsSequencer.ContainsKey(id))
			{
				ClipsSequencer[id] = 0;
			}
		}
	}

}