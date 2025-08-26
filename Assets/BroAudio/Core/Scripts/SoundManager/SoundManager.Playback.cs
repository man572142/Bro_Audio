using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Utility;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        private readonly Queue<IPlayable> _playbackQueue = new Queue<IPlayable>();
        private AudioPlayer.PlaybackHandover _playbackHandoverDelegate;

        #region Play
        public IAudioPlayer Play(SoundID id, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, GloballyPlayedPosition, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Vector3 position, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, position, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, position);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Transform followTarget, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, followTarget.position, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, followTarget);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        private bool IsPlayable(SoundID id, IPlayableValidator customValidator, Vector3 position, out IAudioEntity entity, out AudioPlayer player)
        {
            player = null;

            if(!TryGetEntity(id, out entity))
            {
                return false;
            }

            var validator = customValidator ?? entity.PlaybackGroup; // entity's runtime group will be set in InitBank() if it's null
            if (validator != null && !validator.IsPlayable(id, position))
            {
                return false;
            }

            player = _audioPlayerPool.Extract();
            validator?.OnGetPlayer(player);
            return true;
        }

        private IAudioPlayer PlayerToPlay(int id, AudioPlayer player, PlaybackPreference pref)
        {
            BroAudioType audioType = GetAudioType(id);
            var wrapper = new AudioPlayerInstanceWrapper(player);
            player.SetInstanceWrapper(wrapper);
            player.SetPlaybackData(id, pref);

            _playbackQueue.Enqueue(player);

            if (Setting.AlwaysPlayMusicAsBGM && audioType == BroAudioType.Music)
            {
                player.AsBGM().SetTransition(Setting.DefaultBGMTransition, Setting.DefaultBGMTransitionTime);
            }

            // Whether there's any group implementing this or not, we're tracking it anyway
            _combFilteringPreventer ??= new Dictionary<SoundID, AudioPlayer>();
            _combFilteringPreventer[id] = player;

            if (pref.IsLoop(LoopType.SeamlessLoop) || pref.Entity.GetMulticlipsPlayMode() == MulticlipsPlayMode.Chained)
            {
                _playbackHandoverDelegate ??= PlaybackHandover;
                player.OnPlaybackHandover = _playbackHandoverDelegate;
            }
            return wrapper;
        }

        private void PlaybackHandover(int id, InstanceWrapper<AudioPlayer> wrapper, PlaybackPreference pref, EffectType prevTrackEffect, float trackVolume, float pitch)
        {
            var newPlayer = _audioPlayerPool.Extract();
            wrapper.UpdateInstance(newPlayer);
            newPlayer.SetInstanceWrapper(wrapper);

            newPlayer.SetVolume(trackVolume);
            newPlayer.SetPitch(pitch);
            newPlayer.SetPlaybackData(id, pref);
            newPlayer.Play();
            if (pref.ScheduledEndTime > 0d)
            {
                newPlayer.SetScheduledEndTime(pref.ScheduledEndTime);
            }
#if !UNITY_WEBGL
            newPlayer.SetTrackEffect(prevTrackEffect, SetEffectMode.Override);
#endif

            newPlayer.OnPlaybackHandover = PlaybackHandover;
        }

        private void RemoveFromPreventer(AudioPlayer target)
        {
            if(!target.IsActive)
            {
                throw new System.InvalidOperationException("Invalid target player");
            }

            if(_combFilteringPreventer.TryGetValue(target.ID, out var player) && player == target)
            {
                _combFilteringPreventer.Remove(target.ID);
            }
        }

        private void LateUpdate()
        {
            while (_playbackQueue.Count > 0)
            {
                _playbackQueue.Dequeue().Play();
            }
        }
        #endregion

        #region Stop
        public void Stop(BroAudioType targetType)
        {
            Stop(targetType,AudioPlayer.UseEntitySetting);
        }

        public void Stop(int id)
        {
            Stop(id, AudioPlayer.UseEntitySetting);
        }

        public void Stop(int id,float fadeTime)
        {
            StopPlayer<int>(fadeTime, id);
        }

        public void Stop(BroAudioType targetType,float fadeTime)
        {
            StopPlayer<BroAudioType>(fadeTime, (int)targetType.ConvertEverythingFlag());
        }            

        private void StopPlayer<T>(float fadeTime, int identity)
        {
            var players = GetCurrentAudioPlayers();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if(!player.IsActive)
                {
                    continue;
                }

                System.Type type = typeof(T);
                bool isIdAndMatch = type == typeof(int) && player.ID == identity;
                bool isAudioTypeAndMatch = type == typeof(BroAudioType) && ((BroAudioType)identity).Contains(player.ID.ToAudioType());
                if (isIdAndMatch || isAudioTypeAndMatch)
                {
                    player.Stop(fadeTime);
                }
            }
        }
        #endregion

        #region Pause
        public void Pause(int id, bool isPause)
        {
            Pause(id, AudioPlayer.UseEntitySetting, isPause);
        }

        public void Pause(int id, float fadeTime, bool isPause)
        {
            var players = GetCurrentAudioPlayers();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.IsActive && player.ID == id)
                {
                    if (isPause)
                    {
                        player.Pause(fadeTime);
                    }
                    else
                    {
                        player.UnPause(fadeTime);
                    }
                }
            }
        }

        public void Pause(BroAudioType targetType, bool isPause)
        {
            Pause(targetType, AudioPlayer.UseEntitySetting, isPause);
        }

        public void Pause(BroAudioType targetType, float fadeTime, bool isPause)
        {
            targetType = targetType.ConvertEverythingFlag();
            var players = GetCurrentAudioPlayers();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.IsActive && targetType.Contains(player.ID.ToAudioType()))
                {
                    if (isPause)
                    {
                        player.Pause(fadeTime);
                    }
                    else
                    {
                        player.UnPause(fadeTime);
                    }
                }
            }
        } 
        #endregion

        public bool TryGetPreviousPlayerFromCombFilteringPreventer(SoundID id, out AudioPlayer previousPlayer)
        {
            previousPlayer = null;
            return _combFilteringPreventer != null && _combFilteringPreventer.TryGetValue(id, out previousPlayer);
        }
    }
}