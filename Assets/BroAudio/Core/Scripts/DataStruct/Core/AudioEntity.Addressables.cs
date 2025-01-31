#if PACKAGE_ADDRESSABLES
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Ami.BroAudio.Data
{
    public partial class AudioEntity : IEntityIdentity, IAudioEntity
    {
        public bool UseAddressables = false;
        private IList<AudioClip> _batchLoadedClips = null;

        public IEnumerable GetAllAddressableKeys()
        {
            foreach (var clip in Clips)
            {
                yield return clip.AddressableKey;
            }
        }

        public object GetAddressableKey(int index)
        {
            if(index >= 0 && index < Clips.Length)
            {
                return Clips[index].AddressableKey;
            }
            return string.Empty;
        }

        public AsyncOperationHandle<IList<AudioClip>> LoadAssetsAsync()
        {
            if(Clips.Length == 0)
            {
                Debug.LogWarning(Utility.LogTitle + $"{Name.ToWhiteBold()} has no clips to load!");
                return default;
            }

            var op = Addressables.LoadAssetsAsync<AudioClip>(GetAllAddressableKeys(), null, Addressables.MergeMode.Union);
            op.Completed += SetLoadedClipList;
            return op;
        }

        public void SetLoadedClipList(AsyncOperationHandle<IList<AudioClip>> handle)
        {
            handle.Completed -= SetLoadedClipList;

            if(handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Count == Clips.Length)
            {
                _batchLoadedClips = handle.Result;
                for(int i = 0; i < _batchLoadedClips.Count; i++)
                {
                    Clips[i].SetLoadedAsset(_batchLoadedClips[i]);
                }
            }
            else
            {
                throw handle.OperationException;
            }
        }

        public void ReleaseAllAssets()
        {
            bool hasReleased = false;
            if(_batchLoadedClips != null)
            {
                Addressables.Release(_batchLoadedClips);
                hasReleased = true;
            }

            foreach (var clip in Clips)
            {
                clip.SetLoadedAsset(null);
                if(!hasReleased)
                {
                    clip.ReleaseAsset();
                }
            }
        }
    }
}
#endif