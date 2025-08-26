using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Runtime;
using System.Collections.Generic;
using System;
using System.Reflection;
using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        public const string LogTitle = "<b><color=#F3E9D7>[BroAudio] </color></b>";
        public const int UnityEverythingFlag = -1;
        public static Vector3 GloballyPlayedPosition => Vector3.negativeInfinity;

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
        
        public static IAudioEffectModifier CreateAudioEffectProxy<T>(T component) where T : Behaviour
        {
            return component switch
            {
                AudioHighPassFilter highPass => new AudioHighPassFilterProxy(highPass),
                AudioLowPassFilter lowPass => new AudioLowPassFilterProxy(lowPass),
                AudioReverbFilter reverb => new AudioReverbFilterProxy(reverb),
                AudioDistortionFilter distortion => new AudioDistortionFilterProxy(distortion),
                AudioEchoFilter echo => new AudioEchoFilterProxy(echo),
                AudioChorusFilter chorus => new AudioChorusFilterProxy(chorus),
                _ => LogAndReturnNull()
            };
            
            IAudioEffectModifier LogAndReturnNull()
            {
                Debug.LogWarning(Utility.LogTitle + $"No proxy implementation found for {typeof(T).Name}");
                return null;
            }
        }

        public static Type GetFilterTypeFromProxy(IAudioEffectModifier proxy)
        {
            return proxy switch
            {
                AudioHighPassFilterProxy _ => typeof(AudioHighPassFilter),
                AudioLowPassFilterProxy _ => typeof(AudioLowPassFilter),
                AudioReverbFilterProxy _ => typeof(AudioReverbFilter),
                AudioDistortionFilterProxy _ => typeof(AudioDistortionFilter),
                AudioEchoFilterProxy _ => typeof(AudioEchoFilter),
                AudioChorusFilterProxy _ => typeof(AudioChorusFilter),
                _ => LogAndReturnNull()
            };

            Type LogAndReturnNull()
            {
                Debug.LogWarning(Utility.LogTitle + $"No filter type mapping found for {proxy?.GetType().Name}");
                return null;
            }
        }

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

        public static bool IsPlayedGlobally(Vector3 playPos)
        {
            return (GloballyPlayedPosition.IsNegativeInfinity() && playPos.IsNegativeInfinity()) ||
                   (GloballyPlayedPosition.IsPositiveInfinity() && playPos.IsPositiveInfinity());
        }
        
        public static bool IsPositiveInfinity(this Vector3 v)
        {
            return float.IsPositiveInfinity(v.x) && float.IsPositiveInfinity(v.y) && float.IsPositiveInfinity(v.z);
        }
        
        public static bool IsNegativeInfinity(this Vector3 v)
        {
            return float.IsNegativeInfinity(v.x) && float.IsNegativeInfinity(v.y) && float.IsNegativeInfinity(v.z);
        }
    }
}