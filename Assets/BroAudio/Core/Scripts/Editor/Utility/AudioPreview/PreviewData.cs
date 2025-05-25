using Ami.Extension;
using UnityEngine;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Editor
{
    public class PreviewData
    {
        public AudioClip AudioClip;
        public float Volume = AudioConstant.FullVolume;
        public float Pitch = AudioConstant.DefaultPitch;
        public float StartPosition;
        public float EndPosition;
        public float FadeIn;
        public float FadeOut;

        public PreviewData(AudioClip audioClip, float volume, float pitch, ITransport transport)
        {
            AudioClip = audioClip;
            Volume = volume;
            Pitch = pitch;
            SetTransport(transport);
        }

        public PreviewData(IBroAudioClip broAudioClip, float pitch)
        {
            AudioClip = broAudioClip.GetAudioClip();
            Volume = broAudioClip.Volume;
            Pitch = pitch;
            StartPosition = broAudioClip.StartPosition;
            EndPosition = broAudioClip.EndPosition;
            FadeIn = broAudioClip.FadeIn;
            FadeOut = broAudioClip.FadeOut;
        }

        public void SetTransport(ITransport transport)
        {
            StartPosition = transport.StartPosition;
            EndPosition = transport.EndPosition;
            FadeIn = transport.FadeIn;
            FadeOut = transport.FadeOut;
        }

        public float Duration => AudioClip.length - EndPosition - StartPosition;
        public int AbsoluteEndSamples => GetSample(AudioClip.frequency, AudioClip.length - EndPosition);
        public int AbsoluteStartSamples => GetSample(AudioClip.frequency, StartPosition);
    }
}