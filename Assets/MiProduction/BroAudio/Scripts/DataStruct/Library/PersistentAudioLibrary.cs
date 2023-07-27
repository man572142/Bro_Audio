using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.BroAudio.Data
{
    [System.Serializable]
    public class PersistentAudioLibrary : AudioLibrary
    {
        public override BroAudioType PossibleFlags => BroAudioType.Music | BroAudioType.Ambience;

        public bool Loop;
        public bool SeamlessLoop;
        public float TransitionTime;

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

        [SerializeField] private SeamlessType SeamlessTransitionType;
        [SerializeField] private TempoTransition TransitionTempo;
        public static string NameOf_SeamlessType => nameof(SeamlessTransitionType);
        public static string NameOf_TransitionTempo => nameof(TransitionTempo);
#endif
    }
}