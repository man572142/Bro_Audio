using System;
using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public delegate void RequestClipPreview(string clipPath, PreviewRequest request);
    public class PreviewRequest
    {
        public PreviewStrategyType StrategyType;
        public AudioClip AudioClip;
        public float ClipVolume = AudioConstant.FullVolume;
        public float MasterVolume = AudioConstant.FullVolume;
        public float BaseMasterVolume = AudioConstant.FullVolume;
        public float Pitch = AudioConstant.DefaultPitch;
        public float BasePitch = AudioConstant.DefaultPitch;
        public float StartPosition;
        public float EndPosition;
        public float FadeIn;
        public float FadeOut;
        public double PreciseAudioClipLength;
        
        public PreviewRequest(AudioClip audioClip)
        {
            AudioClip = audioClip;
            PreciseAudioClipLength = audioClip.GetPreciseLength();
            ClipVolume = AudioConstant.FullVolume;
        }

        public PreviewRequest(AudioClip audioClip, float volume, ITransport transport)
        {
            AudioClip = audioClip;
            PreciseAudioClipLength = audioClip.GetPreciseLength();
            ClipVolume = volume;
            StartPosition = transport.StartPosition;
            EndPosition = transport.EndPosition;
            FadeIn = transport.FadeIn;
            FadeOut = transport.FadeOut;
        }

        public PreviewRequest(IBroAudioClip broAudioClip)
        {
            SetClip(broAudioClip);
        }

        private void SetClip(IBroAudioClip broAudioClip)
        {
            AudioClip = broAudioClip.GetAudioClip();
            PreciseAudioClipLength = AudioClip.GetPreciseLength();
            ClipVolume = broAudioClip.Volume;
            StartPosition = broAudioClip.StartPosition;
            EndPosition = broAudioClip.EndPosition;
            FadeIn = broAudioClip.FadeIn;
            FadeOut = broAudioClip.FadeOut;
        }

        public double Duration => (PreciseAudioClipLength - StartPosition - EndPosition) / Pitch;
        public double NonPitchDuration => PreciseAudioClipLength - StartPosition - EndPosition;
        public double AbsoluteEndPosition => PreciseAudioClipLength - EndPosition;
        public float Volume => ClipVolume * MasterVolume;

        public void SetReplay(ReplayRequest newReplay)
        {
            SetClip(newReplay.Clip);
            MasterVolume = newReplay.MasterVolume;
            Pitch = newReplay.Pitch;
        }

        public void UpdateRandomizedPreviewValue(RandomFlag flag, float newBaseValue)
        {
            switch (flag)
            {
                case RandomFlag.Pitch:
                    if (!Mathf.Approximately(newBaseValue, BasePitch))
                    {
                        float offset = newBaseValue - BasePitch;
                        Pitch += offset;
                        BasePitch = newBaseValue;
                    }
                    break;
                case RandomFlag.Volume:
                    if (!Mathf.Approximately(newBaseValue, BaseMasterVolume))
                    {
                        float offset = newBaseValue - BaseMasterVolume;
                        MasterVolume += offset;
                        BaseMasterVolume = newBaseValue;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(flag), flag, null);
            }
        }
    }

    public static class PreviewRequestFactory
    {
        public static PreviewRequest CreatePreviewRequest(this Event evt, AudioClip audioClip)
        {
            return new PreviewRequest(audioClip) { StrategyType = evt.GetPreviewStrategyType()};
        }

        public static PreviewRequest CreatePreviewRequest(this Event evt, AudioClip audioClip, float volume, ITransport transport)
        {
            return new PreviewRequest(audioClip, volume, transport) { StrategyType = evt.GetPreviewStrategyType() };
        }
        
        public static PreviewRequest CreatePreviewRequest(this Event evt, IBroAudioClip broAudioClip)
        {
            return new PreviewRequest(broAudioClip) { StrategyType = evt.GetPreviewStrategyType()};
        }
    }
}