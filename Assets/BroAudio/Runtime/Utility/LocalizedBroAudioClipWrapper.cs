#if PACKAGE_LOCALIZATION
using UnityEngine;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Wraps a <see cref="BroAudioClip"/> (supplying playback properties) and an externally
    /// resolved <see cref="AudioClip"/> (from the Unity Asset Table) so that the ScriptableObject
    /// data is never mutated at runtime.
    /// </summary>
    public class LocalizedBroAudioClipWrapper : IBroAudioClip
    {
        private readonly BroAudioClip _broAudioClip;
        private readonly AudioClip _resolvedClip;

        public LocalizedBroAudioClipWrapper(BroAudioClip broAudioClip, AudioClip resolvedClip)
        {
            _broAudioClip = broAudioClip;
            _resolvedClip = resolvedClip;
        }

        public AudioClip GetAudioClip() => _resolvedClip;
        public bool IsValid() => _resolvedClip != null;
        public bool IsSet => _resolvedClip != null;

        public float Volume => _broAudioClip.Volume;
        public float Delay => _broAudioClip.Delay;
        public float StartPosition => _broAudioClip.StartPosition;
        public float EndPosition => _broAudioClip.EndPosition;
        public float FadeIn => _broAudioClip.FadeIn;
        public float FadeOut => _broAudioClip.FadeOut;
    }
}
#endif
