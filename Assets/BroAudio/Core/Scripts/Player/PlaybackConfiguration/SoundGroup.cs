using System;
using System.Collections.Generic;
using Ami.BroAudio.Data;
using Ami.BroAudio.Runtime;
using Ami.Extension;
using UnityEngine;


namespace Ami.BroAudio
{
    [CreateAssetMenu(menuName = nameof(BroAudio) + "/Sound Group", fileName = "SoundGroup", order = 0)]
    public class SoundGroup : ScriptableObject, IPlayableValidator
    {
        [Flags]
        public enum OverrideOption
        {
            None = 0, // will still use the values from DefaultSoundGroup.
            MaxPlayableCount = 1 << 0,
            CombFilteringTime = 1 << 1,

            All = MaxPlayableCount | CombFilteringTime,
        }

        public static SoundGroup DefaultGroup => SoundManager.Instance.Setting.DefaultSoundGroup;

        [field: SerializeField] public int MaxPlayableCount { get; private set; }
        [field: SerializeField] public float CombFilteringTime { get; private set; } = RuntimeSetting.FactorySettings.CombFilteringPreventionInSeconds;
        [field: SerializeField] public OverrideOption OverrideOptions { get; private set; } = OverrideOption.All;

        private int _currentPlayingCount;

        public void OnGetPlayer(IAudioPlayer player)
        {
            _currentPlayingCount++;
            player.OnEnd(_ => _currentPlayingCount--);
        }

        public bool IsPlayable(SoundID id)
        {
            int flag = 1;
            while(flag < (int)OverrideOption.All)
            {
                if(!IsPlayable(id, (OverrideOption)flag))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsPlayable(SoundID id, OverrideOption option)
        {
            if(option == OverrideOption.None || option == OverrideOption.All)
            {
                throw new InvalidOperationException();
            }

            if(ShouldUseDefaultGroupValue(option))
            {
                if(DefaultGroup.OverrideOptions != OverrideOption.All)
                {
                    // TODO: use BroAudioException?
                    throw new FormatException($"The DefaultSoundGroup {nameof(OverrideOptions)} must be {OverrideOption.All}!");
                }
                return DefaultGroup.IsPlayable(id, option);
            }

            switch (option)
            {
                case OverrideOption.MaxPlayableCount:
                    return MaxPlayableCount <= 0 || _currentPlayingCount < MaxPlayableCount;
                case OverrideOption.CombFilteringTime:
                    if (!SoundManager.Instance.HasPassCombFilteringPreventionTime(id, CombFilteringTime))
                    {
#if UNITY_EDITOR
                        if (SoundManager.Instance.Setting.LogCombFilteringWarning)
                        {
                            Debug.LogWarning(Utility.LogTitle + $"One of the plays of Audio:{((SoundID)id).ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                            $"For more information, please go to the [Comb Filtering] section in Tools/BroAudio/Preference.");
                        }
#endif
                        return false;
                    }
                    break;
            }
            return true;
        }

        // The DefaultSoundGroup should be OverrideOption.All, so it always reurn false here 
        // TODO: use editor code to ensure this
        private bool ShouldUseDefaultGroupValue(OverrideOption option)
        {
            return !OverrideOptions.Contains(option);
        }


        private void OnEnable()
        {
            _currentPlayingCount = 0;
        }
    }
}