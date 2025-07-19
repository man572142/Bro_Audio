using UnityEngine;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        public static IBroAudioClip PickNewOne(this BroAudioClip[] clips, MulticlipsPlayMode playMode, int id, out int index, int contextValue = 0)
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

            var context = new ClipSelectionContext { Id = id, Value = contextValue };
            var strategy = playMode.GetClipSelectionStrategy();

            return strategy.SelectClip(clips, context, out index);
        }
    }
}