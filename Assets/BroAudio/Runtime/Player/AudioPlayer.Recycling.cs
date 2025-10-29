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
            DestroyAddedEffectComponents();
            ClearEvents();

            if (TryGetMixerAndTrack(out _, out var track))
            {
                MixerPool.ReturnTrack(TrackType, track);
                TrackType = AudioTrackType.Generic;
            }
            MixerPool.ReturnPlayer(this);

            if (_decorators != null)
            {
                foreach (var decorator in _decorators)
                {
                    decorator.Recycle();
                }
            }
            _decorators = null;

            _instanceWrapper?.Recycle();
            _instanceWrapper = null;

            OnPlaybackHandover = null;
            AudioTrack = null;

            ID = SoundID.Invalid;
        }

        private void ClearEvents()
        {
            _onStart = null;
            _onUpdate = null;
            _onEnd = null;
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

        private void DestroyAddedEffectComponents()
        {
            if (_addedEffects != null)
            {
                foreach (var effect in _addedEffects)
                {
                    if (effect.Component)
                    {
                        Destroy(effect.Component);
                    }
                }
                _addedEffects = null;
            }
        }
    }
}