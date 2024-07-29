using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        private static Dictionary<int, int> ClipsSequencer = null;

        public static BroAudioClip PickNewOne(this BroAudioClip[] clips, MulticlipsPlayMode playMode, int id, out int index)
        {
            index = 0;
            if (clips == null || clips.Length <= 0)
            {
                Debug.LogError(LogTitle + "There are no AudioClip in the entity");
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
                case MulticlipsPlayMode.Shuffle:
                    return clips.PickShuffleClip(out index);
            }
            return default;
        }

        private static BroAudioClip PickNextClip(this BroAudioClip[] clips, int id, out int index)
        {
            ClipsSequencer ??= new Dictionary<int, int>();

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

        private static BroAudioClip PickRandomClip(this BroAudioClip[] clips, out int index)
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

        private static BroAudioClip PickShuffleClip(this BroAudioClip[] clips, out int index)
        {
            index = Random.Range(0, clips.Length);
            BroAudioClip result;

            if(clips.Use(index, out result))
            {
                return result;
            }

            // to avoid overusing the Random method when there are only a few clips left
            bool isIncreasing = Random.Range(0,1) == 0 ? false : true;
            for(int i = 0; i < clips.Length; i++)
            {
                index += isIncreasing ? 1 : -1;
                index = (index + clips.Length) % clips.Length;

                if (clips.Use(index, out result))
                {
                    return result;
                }
            }

            // reset all IsUsed and pick randomly again
            clips.ResetIsUse();
            index = Random.Range(0, clips.Length);
            if(clips.Use(index, out result))
            {
                return result;
            }
            return null;
        }

        private static bool Use(this BroAudioClip[] clips, int index, out BroAudioClip result)
        {
            result = clips[index];
            if (!result.IsUsed)
            {
                result.IsUsed = true;
                return true;
            }
            result = null;
            return false;
        }

        public static void ResetIsUse(this BroAudioClip[] clips)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].IsUsed = false;
            }
        }

#if UNITY_EDITOR
        public static void ClearPreviewAudioData()
        {
            ClipsSequencer?.Clear();
            ClipsSequencer = null;
        }
#endif
    }
}