using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        private InstanceWrapper<AudioPlayer> _instanceWrapper;

        internal void SetInstanceWrapper(InstanceWrapper<AudioPlayer> instance)
        {
            _instanceWrapper = instance;
        }

        internal IAudioPlayer GetInstanceWrapper()
        {
            return _instanceWrapper as IAudioPlayer;
        }

        public void Recycle()
        {
            ResetAudioSource();
            DestroyAudioFilterReader();
            _onUpdate = null;

            if(TryGetMixerAndTrack(out _, out var track))
            {
                MixerPool.ReturnTrack(TrackType, track);
                TrackType = AudioTrackType.Generic;
            }
            MixerPool.ReturnPlayer(this);

            if(_decorators != null)
            {
                foreach(var decorator in _decorators)
                {
                    decorator.Recycle();
                }
            }
            _decorators = null;

            if (OnSeamlessLoopReplay == null)
            {
                _instanceWrapper.Recycle();
            }
            _instanceWrapper = null;

            OnSeamlessLoopReplay = null;
            AudioTrack = null;

            ID = -1;
        }

        private void ResetAudioSource()
        {
            if(!AudioSource)
            {
                Debug.LogError(Utility.LogTitle + $" AudioSource is missing!");
                return;
            }

            _proxy?.Dispose();
            _proxy = null;
        }

        private void DestroyAudioFilterReader()
        {
            if(_audioFilterReader)
            {
                Destroy(_audioFilterReader);
            }
        }
    }
}