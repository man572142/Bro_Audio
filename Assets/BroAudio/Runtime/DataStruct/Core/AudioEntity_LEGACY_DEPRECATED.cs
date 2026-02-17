using System.Collections.Generic;
using Ami.BroAudio.Runtime;
using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Data
{
    [System.Obsolete("AudioEntity_LEGACY is no longer used. Use AudioEntity instead.", true)]
    [System.Serializable]
    internal class AudioEntity_LEGACY
    {
        [field: SerializeField]
        public string Name { get; private set; }

        [field: SerializeField]
        public int ID { get; private set; }

        [SerializeField] public MulticlipsPlayMode MulticlipsPlayMode = MulticlipsPlayMode.Single;
        [SerializeField] public PlaybackGroup _group;

        private PlaybackGroup _upperGroup;

        public BroAudioClip[] Clips;

        [field: SerializeField] public float MasterVolume { get; private set; }
        [field: SerializeField] public bool Loop { get; private set; }
        [field: SerializeField] public bool SeamlessLoop { get; private set; }
        [field: SerializeField] public float TransitionTime { get; private set; }
        [field: SerializeField] public SpatialSetting SpatialSetting { get; private set; }
        [field: SerializeField] public int Priority { get; private set; }
        [field: SerializeField] public float Pitch { get; private set; }
        [field: SerializeField] public float PitchRandomRange { get; private set; }
        [field: SerializeField] public float VolumeRandomRange { get; private set; }
        [field: SerializeField] public RandomFlag RandomFlags { get; private set; }

#if UNITY_EDITOR
        [SerializeField] public SeamlessType SeamlessTransitionType = SeamlessType.ClipSetting;
        [SerializeField] public TempoTransition TransitionTempo = default;
        [SerializeField] public bool SnapToFullVolume = false;
#endif

#if PACKAGE_ADDRESSABLES
        public bool UseAddressables = false;
#endif
    }
}