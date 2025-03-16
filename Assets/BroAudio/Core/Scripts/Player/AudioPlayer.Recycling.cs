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

        public void Recycle()
        {
            ResetAudioSource();
            DestroyAudioFilterReader();

            TrackType = AudioTrackType.Generic;
            _onUpdate = null;
#if !UNITY_WEBGL
            Mixer.ReturnTrack(TrackType, AudioTrack);
#endif
            Mixer.ReturnPlayer(this);
            if (OnSeamlessLoopReplay == null)
            {
                _instanceWrapper.Recycle();
            }
            _instanceWrapper = null;
            OnSeamlessLoopReplay = null;
            AudioTrack = null;
            _decorators = null;
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