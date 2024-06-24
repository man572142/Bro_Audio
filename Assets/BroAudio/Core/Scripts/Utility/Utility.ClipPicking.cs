using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
	public static partial class Utility
	{
		public static Dictionary<int, int> ClipsSequencer = new Dictionary<int, int>();

		public static BroAudioClip PickNewOne(this BroAudioClip[] clips, MulticlipsPlayMode playMode, int id, out int index)
		{
			index = 0;
            if (clips == null || clips.Length <= 0)
			{
				Debug.LogError(LogTitle + "There is no AudioClip in asset");
				return null;
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
					return clips.PickNextClip(id, out index);
				case MulticlipsPlayMode.Random:
					return clips.PickRandomClip(out index);
			}
			return default;
		}

		private static BroAudioClip PickNextClip(this BroAudioClip[] clips, int id, out int index)
		{
			index = 0;
			if (ClipsSequencer.ContainsKey(id))
			{
				ClipsSequencer[id] = ClipsSequencer[id] + 1 >= clips.Length ? 0 : ClipsSequencer[id] + 1;
				index = ClipsSequencer[id];
			}
			else
			{
				ClipsSequencer.Add(id, 0);
			}
			return clips[index];
		}

		public static BroAudioClip PickRandomClip(this BroAudioClip[] clips, out int index)
		{
            index = 0;
            int totalWeight = clips.Sum(x => x.Weight);

			// No Weight
			if (totalWeight == 0)
			{
				index = Random.Range(0, clips.Length);
                return clips[index];
			}

			// Use Weight
			int targetWeight = Random.Range(0, totalWeight);
			int sum = 0;

			for (int i = 0; i < clips.Length; i++)
			{
				sum += clips[i].Weight;
				if (targetWeight < sum)
				{
					index = i;
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