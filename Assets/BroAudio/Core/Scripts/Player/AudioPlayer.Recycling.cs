using UnityEngine;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
	{
        private void Recycle()
        {
            ResetAudioSource();
            DestroyAudioFilterReader();

            _onUpdate = null;
            OnRecycle?.Invoke(this);

            TrackType = AudioTrackType.Generic;
            AudioTrack = null;
            _decorators = null;
        }

        /// <summary>
        /// Force to reset AudioSource
        /// </summary>
        private void ResetAudioSource()
        {
            if(!AudioSource)
            {
                Debug.LogError(Utility.LogTitle + $" AudioSource is missing!");
                return;
            }

            if(!_hasUserGotAudioSource)
            {
                Debug.LogWarning(Utility.LogTitle + " Reseting AudioSource without ever calling GetAudioSource() is unnecessary");
                return;
            }

            AudioSource.bypassEffects = false;
            AudioSource.bypassListenerEffects = false;
            AudioSource.bypassReverbZones = false;
            AudioSource.clip = null;
            AudioSource.dopplerLevel = 1f;

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