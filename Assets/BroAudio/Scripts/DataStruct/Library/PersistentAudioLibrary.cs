using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public class PersistentAudioLibrary : AudioLibrary
    {
        //public override BroAudioType PossibleFlags => BroAudioType.Music | BroAudioType.Ambience;

        public bool Loop = false;
        public bool SeamlessLoop = false;
        public float TransitionTime = default;

#if UNITY_EDITOR
        public enum SeamlessType
        {
            ClipSetting,
            Time,
            Tempo
        }

        [System.Serializable]
        public struct TempoTransition
        {
            public float BPM;
            public int Beats;
        }

        [SerializeField] private SeamlessType SeamlessTransitionType = SeamlessType.ClipSetting;
        [SerializeField] private TempoTransition TransitionTempo = default;
        public static string NameOf_SeamlessType => nameof(SeamlessTransitionType);
        public static string NameOf_TransitionTempo => nameof(TransitionTempo);
#endif
    }
}