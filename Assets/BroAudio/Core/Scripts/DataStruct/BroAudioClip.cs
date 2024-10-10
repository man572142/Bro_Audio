using UnityEngine;
#if PACKAGE_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace Ami.BroAudio.Data
{
	[System.Serializable]
	public class BroAudioClip : IBroAudioClip
	{
		[SerializeField] internal AudioClip AudioClip;
#if PACKAGE_ADDRESSABLES
        public AssetReferenceT<AudioClip> AudioClipAssetReference;
#endif
		public float Volume;
		public float Delay;
		public float StartPosition;
		public float EndPosition;
		public float FadeIn;
		public float FadeOut;

		// For random, velocity
		public int Weight;

		// For shuffle (runtime-only)
		[System.NonSerialized]
		public bool IsUsed;

		AudioClip IBroAudioClip.AudioClip
        {
            get
            {
                if(AudioClip != null)
                {
                    return AudioClip;
                }
#if PACKAGE_ADDRESSABLES
                if(AudioClipAssetReference != null && !string.IsNullOrEmpty(AudioClipAssetReference.AssetGUID))
                {
#if UNITY_EDITOR
                    return AudioClipAssetReference.editorAsset;
#else
                    return AudioClipAssetReference.Asset as AudioClip;
#endif
                }
#endif
                return null;
            }
        }
		float IBroAudioClip.Volume => Volume;
		float IBroAudioClip.Delay => Delay;
		float IBroAudioClip.StartPosition => StartPosition;
		float IBroAudioClip.EndPosition => EndPosition;
		float IBroAudioClip.FadeIn => FadeIn;
		float IBroAudioClip.FadeOut => FadeOut;
        public int Velocity => Weight;
		public bool IsNull() => AudioClip == null;
	}

	public interface IBroAudioClip
	{
		AudioClip AudioClip { get; }
		float Volume { get; }
		float Delay { get; }
		float StartPosition { get; }
		float EndPosition { get; }
		float FadeIn { get;}
		float FadeOut { get; }
	}
}