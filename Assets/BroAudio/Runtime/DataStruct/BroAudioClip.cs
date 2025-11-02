using Ami.Extension;
using UnityEngine;
#if PACKAGE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Ami.BroAudio.Data
{
    [System.Serializable]
    public partial class BroAudioClip : IBroAudioClip
    {
        [SerializeField] private AudioClip AudioClip;

        public float Volume = AudioConstant.FullVolume;
        public float Delay;
        public float StartPosition;
        public float EndPosition;
        public float FadeIn;
        public float FadeOut;

        // For random, velocity
        public int Weight;

        float IBroAudioClip.Volume => Volume;
        float IBroAudioClip.Delay => Delay;
        float IBroAudioClip.StartPosition => StartPosition;
        float IBroAudioClip.EndPosition => EndPosition;
        float IBroAudioClip.FadeIn => FadeIn;
        float IBroAudioClip.FadeOut => FadeOut;
        public int Velocity => Weight;

        public bool IsValid()
        {
            if(AudioClip != null)
            {
                return true;
            }
            return IsAddressablesAvailable();
        }

        public bool IsSet
        {
            get
            {
                if(AudioClip != null)
                {
                    return true;
                }
                return IsAddressablesAvailable();
            }
        }

#if !PACKAGE_ADDRESSABLES
        public AudioClip GetAudioClip() => AudioClip;
        public bool IsAddressablesAvailable() => false;
#endif

        public static class NameOf
        {
            public const string AudioClip = nameof(AudioClip);
            public const string AudioClipAssetReference = nameof(AudioClipAssetReference);
        }
    }

    public interface IBroAudioClip
    {
        AudioClip GetAudioClip();
        bool IsValid();
        bool IsSet { get; }

        float Volume { get; }
        float Delay { get; }
        float StartPosition { get; }
        float EndPosition { get; }
        float FadeIn { get;}
        float FadeOut { get; }
    }
}