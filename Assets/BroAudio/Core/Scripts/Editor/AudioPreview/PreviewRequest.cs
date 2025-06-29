using Ami.BroAudio.Data;
using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Editor
{
    public class PreviewRequest
    {
        public AudioClip AudioClip;
        public float ClipVolume = AudioConstant.FullVolume;
        public float MasterVolume = AudioConstant.FullVolume;
        public float StartPosition;
        public float EndPosition;
        public float FadeIn;
        public float FadeOut;
        public float Pitch = AudioConstant.DefaultPitch;

        public PreviewRequest(AudioClip audioClip, float volume, Transport transport)
        {
            AudioClip = audioClip;
            ClipVolume = volume;
            StartPosition = transport.StartPosition;
            EndPosition = transport.EndPosition;
            FadeIn = transport.FadeIn;
            FadeOut = transport.FadeOut;
        }

        public PreviewRequest(IBroAudioClip broAudioClip)
        {
            AudioClip = broAudioClip.GetAudioClip();
            ClipVolume = broAudioClip.Volume;
            StartPosition = broAudioClip.StartPosition;
            EndPosition = broAudioClip.EndPosition;
            FadeIn = broAudioClip.FadeIn;
            FadeOut = broAudioClip.FadeOut;
        }

        public double Duration => ((double)AudioClip.samples / AudioClip.frequency - StartPosition - EndPosition) / Pitch;
        public float Volume => ClipVolume * MasterVolume;

        public void SetReplay(ReplayData newReplay)
        {
            AudioClip = newReplay.Clip.GetAudioClip();
            MasterVolume = newReplay.MasterVolume;
            Pitch = newReplay.Pitch;
        }
    }
}