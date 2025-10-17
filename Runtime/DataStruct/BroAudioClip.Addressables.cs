#if PACKAGE_ADDRESSABLES
using Ami.BroAudio.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Ami.BroAudio.Data
{
	public partial class BroAudioClip : IBroAudioClip
	{
        [SerializeField] private AssetReferenceT<AudioClip> AudioClipAssetReference;
        private AudioClip _loadedAsset = null;

        public IKeyEvaluator AddressableKey => AudioClipAssetReference;
        public bool IsLoaded => _loadedAsset != null || AudioClip != null || AudioClipAssetReference.Asset != null;

        public AsyncOperationHandle<AudioClip> LoadAssetAsync()
        {
            return AudioClipAssetReference.LoadAssetAsync();
        }

        public void ReleaseAsset()
        {
            if(AudioClipAssetReference.OperationHandle.IsValid())
            {
                Addressables.Release(AudioClipAssetReference.OperationHandle);
            }
        }

        public void SetLoadedAsset(AudioClip clip)
        {
            _loadedAsset = clip;
        }

        public AudioClip GetAudioClip()
        {
            if (AudioClip != null)
            {
                return AudioClip;
            }
            if(_loadedAsset != null)
            {
                return _loadedAsset;
            }

            string assetIdentity = null;
            if (IsAddressablesAvailable())
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    return AudioClipAssetReference.editorAsset;
                }
                assetIdentity = AudioClipAssetReference.editorAsset.name;
#else
                assetIdentity = AudioClipAssetReference.AssetGUID;
#endif
                if (AudioClipAssetReference.Asset is AudioClip clip) // null check
                {
                    return clip;
                }
                throw new BroAudioException($"AudioClip [<b>{assetIdentity}</b>] is marked as Addressables, but it hasn't been loaded");
            }
            return AudioClip;
        }

        public bool IsAddressablesAvailable() => !string.IsNullOrEmpty(AudioClipAssetReference.AssetGUID);
    }
}
#endif