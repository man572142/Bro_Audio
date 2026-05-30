using Ami.Extension;
using UnityEngine;

namespace Ami.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IPlayable, IRecyclable<AudioPlayer>
    {
        // When an AudioSource is virtualized by Unity's voice manager it becomes inaudible,
        // yet it still holds an AudioMixerGroup (track). Tracks are a scarce, prebuilt resource
        // (MaxRealVoices + VirtualTrackCount), so we lend the track back to the pool while the
        // player is virtual and reclaim one once it becomes audible again.
        private bool _isTrackReleasedWhileVirtual = false;

#if !UNITY_WEBGL
        private void UpdateTrackVirtualization()
        {
            // isVirtual is only meaningful for a source that is actively playing.
            // Skip while stopping so the fade-out teardown keeps its track.
            // Dominators are meant to stay audible and their effect automation is tied to a
            // dedicated track, so only the abundant Generic tracks are lent back here.
            if (TrackType != AudioTrackType.Generic || !HasStartedPlaying || IsStopping || !AudioSource.isPlaying)
            {
                return;
            }

            bool isVirtual = AudioSource.isVirtual;
            if (isVirtual && !_isTrackReleasedWhileVirtual)
            {
                ReleaseTrackWhileVirtual();
            }
            else if (!isVirtual && _isTrackReleasedWhileVirtual)
            {
                ReclaimTrackFromVirtual();
            }
        }

        private void ReleaseTrackWhileVirtual()
        {
            if (!TryGetMixerAndTrack(out var mixer, out var track))
            {
                return;
            }

            // Mute the returned track's channels so the pooled track is clean for its next owner,
            // mirroring the teardown done in ResetVolume()/ResetEffect() on a normal recycle.
            if (IsUsingTrackEffect)
            {
                mixer.SafeSetFloat(GetSendParaName(), AudioConstant.MinDecibelVolume);
            }
            mixer.SafeSetFloat(GetCurrentTrackName(), AudioConstant.MinDecibelVolume);

            MixerPool?.ReturnTrack(TrackType, track);
            AudioTrack = null; // clears outputAudioMixerGroup, _currTrackName and _sendParaName
            _mixerDecibelVolume = UnSetMixerDecibelVolume;
            _isTrackReleasedWhileVirtual = true;
        }

        private void ReclaimTrackFromVirtual()
        {
            var track = MixerPool?.GetTrack(TrackType);
            if (track == null)
            {
                // No track available right now. Stay released and retry on a later frame;
                // audible players are served first while tracks are scarce.
                return;
            }

            AudioTrack = track; // _currTrackName/_sendParaName were nulled on release, so they recompute
            _mixerDecibelVolume = UnSetMixerDecibelVolume;
            // The fallback path used while track-less drives AudioSource.volume directly;
            // reset it so the reclaimed mixer track is the sole volume authority again.
            AudioSource.volume = AudioConstant.FullVolume;

            // Restore the effect routing: the direct channel stays muted and the volume is
            // sent through the effect channel, matching SetTrackEffect()'s ChangeChannel().
            if (IsUsingTrackEffect && TryGetMixerAndTrack(out var mixer, out _))
            {
                mixer.SafeSetFloat(GetCurrentTrackName(), AudioConstant.MinDecibelVolume);
            }

            _isTrackReleasedWhileVirtual = false;
            UpdateVolume(); // writes the current volume to VolumeParaName on the reclaimed track
        }
#endif
    }
}
