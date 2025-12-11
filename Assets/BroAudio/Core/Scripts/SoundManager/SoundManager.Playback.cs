using System;
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
        private Action<IAudioPlayer> _onPlayerStart;

        #region Play
        public IAudioPlayer Play(SoundID id, float fadeIn, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, GloballyPlayedPosition, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity).SetNextFadeIn(fadeIn);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Vector3 position, float fadeIn, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, position, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, position).SetNextFadeIn(fadeIn);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Transform followTarget, float fadeIn, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, followTarget.position, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, followTarget).SetNextFadeIn(fadeIn);
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
            _onPlayerStart ??= AddToCombFilteringPreventer;
            player.OnStart(_onPlayerStart);
            
            if (pref.IsLoop(LoopType.SeamlessLoop) || pref.Entity.PlayMode == MulticlipsPlayMode.Chained)
            {
                _playbackHandoverDelegate ??= PlaybackHandover;
                player.OnPlaybackHandover = _playbackHandoverDelegate;
            }
            return wrapper;
        }

        private void AddToCombFilteringPreventer(IAudioPlayer player)
        {
            _combFilteringPreventer ??= new Dictionary<SoundID, AudioPlayer>();
            _combFilteringPreventer[player.ID] = player as AudioPlayer;
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
            Stop(targetType,FadeData.UseClipSetting);
        }

        public void Stop(int id)
        {
            Stop(id, FadeData.UseClipSetting);
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
            Pause(id, FadeData.UseClipSetting, isPause);
        }

        public void Pause(int id, float fadeTime, bool isPause)
        {
            var players = GetCurrentAudioPlayers();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.IsActive && player.ID == id)
                {
                    PausePlayer(player, isPause, fadeTime);
                }
            }
        }

        public void Pause(BroAudioType targetType, bool isPause)
        {
            Pause(targetType, FadeData.UseClipSetting, isPause);
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
                    PausePlayer(player, isPause, fadeTime);
                }
            }
        }

        private static void PausePlayer(AudioPlayer player, bool isPause, float fadeTime)
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
        #endregion

        public bool TryGetPreviousPlayerFromCombFilteringPreventer(SoundID id, out AudioPlayer previousPlayer)
        {
            previousPlayer = null;
            return _combFilteringPreventer != null && _combFilteringPreventer.TryGetValue(id, out previousPlayer);
        }
    }
}