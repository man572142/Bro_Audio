using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Runtime;
using System.Collections.Generic;
using System;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        public const string LogTitle = "<b><color=#F3E9D7>[BroAudio] </color></b>";
        public const int UnityEverythingFlag = -1;

        #region Efficient HasFlag
        // faster than Enum.HasFlag, could be used in runtime.
        public static bool Contains(this BroAudioType flags, BroAudioType targetFlag)
        {
            return ((int)flags & (int)targetFlag) != 0;
        }

        public static bool Contains(this RandomFlag flags, RandomFlag targetFlag)
        {
            return ((int)flags & (int)targetFlag) != 0;
        }
        #endregion

        public static BroAudioType ConvertEverythingFlag(this BroAudioType audioType)
        {
            if((int)audioType == UnityEverythingFlag)
            {
                return BroAudioType.All;
            }
            return audioType;
        }

        public static float GetDeltaTime()
        {
            var updateMode = SoundManager.Instance.Setting.UpdateMode;
            if(updateMode == UnityEngine.Audio.AudioMixerUpdateMode.UnscaledTime)
            {
                return Time.unscaledDeltaTime;
            }
            return Time.deltaTime;
        }

        public static int GetSample(int sampleRate, float seconds)
        {
            return (int)(sampleRate * seconds);
        }

        public static bool IsDefaultCurve(this AnimationCurve curve , float defaultValue)
        {
            if(curve == null || curve.length == 0)
            {
                return true;
            }
            else if(curve.length == 1 && curve[0].value == defaultValue)
            {
                return true;
            }
            return false;
        }

        public static void SetCustomCurveOrResetDefault(this AudioSource audioSource, AnimationCurve curve, AudioSourceCurveType curveType)
        {
            if(curveType == AudioSourceCurveType.CustomRolloff)
            {
                Debug.LogError(LogTitle + $"Don't use this method on {AudioSourceCurveType.CustomRolloff}, please use RolloffMode to detect if is default or not");
                return;
            }

            float defaultValue = GetCurveDefaultValue(curveType);

            if (!curve.IsDefaultCurve(defaultValue))
            {
                audioSource.SetCustomCurve(curveType,curve);
            }
            else
            {
                switch (curveType)
                {
                    case AudioSourceCurveType.SpatialBlend:
                        audioSource.spatialBlend = defaultValue;
                        break;
                    case AudioSourceCurveType.ReverbZoneMix:
                        audioSource.reverbZoneMix = defaultValue;
                        break;
                    case AudioSourceCurveType.Spread:
                        audioSource.spread = defaultValue;
                        break;
                }
            }
        }

        public static float GetCurveDefaultValue(AudioSourceCurveType curveType) => curveType switch
        {
            AudioSourceCurveType.SpatialBlend => AudioConstant.SpatialBlend_2D,
            AudioSourceCurveType.ReverbZoneMix => AudioConstant.DefaultReverZoneMix,
            AudioSourceCurveType.Spread => AudioConstant.DefaultSpread,
            _ => default,
        };

        internal static T GetOrCreateDecorator<T>(ref List<AudioPlayerDecorator> list, Func<T> onCreateDecorator) where T : AudioPlayerDecorator
        {
            if (list != null && list.TryGetDecorator(out T decoratePalyer))
            {
                return decoratePalyer;
            }

            decoratePalyer = onCreateDecorator.Invoke();
            list ??= new List<AudioPlayerDecorator>();
            list.Add(decoratePalyer);
            return decoratePalyer;
        }

        internal static bool TryGetDecorator<T>(this List<AudioPlayerDecorator> list, out T result) where T : AudioPlayerDecorator
        {
            result = null;
            if (list != null)
            {
                foreach (var deco in list)
                {
                    if (deco is T target)
                    {
                        result = target;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}