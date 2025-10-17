using Ami.BroAudio.Data;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    public class RandomClipStrategy : IClipSelectionStrategy
    {
        public IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)
        {
            index = 0;
            int totalWeight = 0;
            foreach (var clip in clips)
            {
                totalWeight += clip.Weight;
            }

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
                    return clips[i];
                }
            }
            return null;
        }
    }
}