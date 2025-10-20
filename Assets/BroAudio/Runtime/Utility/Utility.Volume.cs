using UnityEngine;
using Ami.Extension;
using Ami.BroAudio.Tools;
using static Ami.Extension.AudioConstant;

namespace Ami.BroAudio
{
    public static partial class Utility
    {
        public static readonly float[] BroVolumeSplitPoints = { -80f, -60f, -36f, -24f, -12f, -6f, 0f, 6f, 20f };

        public static float SliderToVolume(SliderType sliderType, float normalizedValue, bool allowBoost)
        {
            normalizedValue = Mathf.Max(normalizedValue, MinVolume);
            switch (sliderType)
            {
                case SliderType.Logarithmic:
                    float max = allowBoost ? MaxLogValue : FullVolumeLogValue;
                    float logValue = Mathf.Lerp(MinLogValue, max, normalizedValue);
                    return Mathf.Pow(10, logValue);
                case SliderType.BroVolume:
                case SliderType.BroVolumeNoField:
                    return SliderToBroVolume(normalizedValue, allowBoost);
                default:
                    return allowBoost ? normalizedValue * MaxVolume : normalizedValue;
            }
        }

        public static float VolumeToSlider(SliderType sliderType, float volume, bool allowBoost)
        {
            float max;
            switch (sliderType)
            {
                case SliderType.Logarithmic:
                    max = allowBoost ? MaxLogValue : FullVolumeLogValue;
                    return Mathf.InverseLerp(MinLogValue, max, Mathf.Log10(volume));
                case SliderType.BroVolume:
                case SliderType.BroVolumeNoField:
                    return BroVolumeToSlider(volume, allowBoost);
                default:
                    max = allowBoost ? MaxVolume : FullVolume;
                    return Mathf.InverseLerp(MinVolume, max, volume);
            }
        }

        public static float BroVolumeToSlider(float vol, bool allowBoost = true)
        {
            float decibelVol = vol.ToDecibel(allowBoost);
            int length = allowBoost ? BroVolumeSplitPoints.Length : GetFullVolumeSplitPointIndex() + 1;
            float volumeStep = GetBroVolumeStep(allowBoost);
            for (int i = 0; i < length; i++)
            {
                if (i + 1 >= length)
                {
                    return 1f;
                }
                else if (decibelVol >= BroVolumeSplitPoints[i] && decibelVol < BroVolumeSplitPoints[i + 1])
                {
                    float currentStepSliderValue = volumeStep * i;
                    float range = Mathf.Abs(BroVolumeSplitPoints[i + 1] - BroVolumeSplitPoints[i]);
                    float stepProgress = Mathf.Abs(decibelVol - BroVolumeSplitPoints[i]) / range;
                    return currentStepSliderValue + stepProgress * volumeStep;
                }
            }
            return 0f;
        }

        public static float SliderToBroVolume(float sliderValue, bool allowBoost = true)
        {
            if (sliderValue == 1f)
            {
                return allowBoost ? MaxVolume : FullVolume;
            }
            float volumeStep = GetBroVolumeStep(allowBoost);
            int newStepIndex = (int)(sliderValue / volumeStep);
            float progress = (sliderValue % volumeStep) / volumeStep;
            float range = Mathf.Abs(BroVolumeSplitPoints[newStepIndex + 1] - BroVolumeSplitPoints[newStepIndex]);
            float decibelResult = BroVolumeSplitPoints[newStepIndex] + range * progress;
            return decibelResult.ToNormalizeVolume(allowBoost);
        }

        public static float GetBroVolumeStep(bool allowBoost)
        {
            int length = allowBoost ? BroVolumeSplitPoints.Length : GetFullVolumeSplitPointIndex() + 1;
            return 1f / (length - 1);
        }

        private static int GetFullVolumeSplitPointIndex()
        {
            for (int i = 0; i < BroVolumeSplitPoints.Length; i++)
            {
                if (BroVolumeSplitPoints[i] == FullDecibelVolume)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}