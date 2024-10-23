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
        private Queue<IPlayable> _playbackQueue = new Queue<IPlayable>();

        #region Play
        public IAudioPlayer Play(int id)
        {
            if (IsPlayable(id,out var entity) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity);
                return PlayerToPlay(id, player, pref);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Vector3 position)
        {
            if (IsPlayable(id,out var entity) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity, position);
                return PlayerToPlay(id, player, pref);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Transform followTarget)
        {
            if (IsPlayable(id,out var entity) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity, followTarget);
                return PlayerToPlay(id, player, pref);
            }
            return null;
        }

        private IAudioPlayer PlayerToPlay(int id, AudioPlayer player, PlaybackPreference pref)
        {
            BroAudioType audioType = GetAudioType(id);
            player.SetPlaybackData(id, pref);

            _playbackQueue.Enqueue(player);
            var wrapper = new AudioPlayerInstanceWrapper(player);

            if (Setting.AlwaysPlayMusicAsBGM && audioType == BroAudioType.Music)
            {
                player.AsBGM().SetTransition(Setting.DefaultBGMTransition, Setting.DefaultBGMTransitionTime);
            }

            if(CombFilteringPreventionInSeconds > 0f)
            {
                _combFilteringPreventer ??= new Dictionary<SoundID, AudioPlayer>();
                player.OnEnd(RemoveFromPreventer);
                _combFilteringPreventer[id] = player;
            }

            if (pref.Entity.SeamlessLoop)
            {
                var seamlessLoopHelper = new SeamlessLoopHelper(wrapper, GetNewAudioPlayer);
                seamlessLoopHelper.AddReplayListener(player);
            }

            //pref.Entity.Config.AddPlayingEntity(wrapper);
            return wrapper;
        }

        private void RemoveFromPreventer(SoundID id)
        {
            if(_combFilteringPreventer != null)
            {
                _combFilteringPreventer.Remove(id);
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
            StopPlayer(fadeTime, id);
        }

        public void Stop(BroAudioType targetType,float fadeTime)
        {
            StopPlayer(fadeTime, targetType);
        }            

        private void StopPlayer<TParameter>(float fadeTime, TParameter parameter)
        {
            var players = GetCurrentAudioPlayers();
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if(!player.IsActive)
                {
                    continue;
                }

                bool isIdAndMatch = parameter is int id && player.ID == id;
                bool isAudioTypeAndMatch = parameter is BroAudioType audioType && audioType.Contains(player.ID.ToAudioType());
                if (isIdAndMatch || isAudioTypeAndMatch)
                {
                    player.Stop(fadeTime);
                }
            }
        }
        #endregion

        public void Pause(int id, bool isPause)
        {
            Pause(id, isPause, AudioPlayer.UseEntitySetting);
        }

        public void Pause(int id, bool isPause, float fadeTime)
        {
            foreach(var player in GetCurrentAudioPlayers())
            {
                if(player.IsActive && player.ID == id)
                {
                    if(isPause)
                    {
                        player.Stop(fadeTime, StopMode.Pause, null);
                    }
                    else
                    {
                        player.UnPause(fadeTime);
                    }
                }
            }
        }


        private bool IsPlayable(int id, out IAudioEntity entity)
        {
            entity = null;
            if (id <= 0 || !_audioBank.TryGetValue(id, out entity))
            {
                Debug.LogError(LogTitle + $"The sound is missing or it has never been assigned. No sound will be played. SoundID:{id}");
                return false;
            }

            // TODO:改成用Config
            //return entity.Config.IsPlayable();
            if (_combFilteringPreventer != null && _combFilteringPreventer.TryGetValue(id, out var previousPlayer)
                && !HasPassPreventionTime(previousPlayer.PlaybackStartingTime))
            {
#if UNITY_EDITOR
                if (Setting.LogCombFilteringWarning)
                {
                    Debug.LogWarning(LogTitle + $"One of the plays of Audio:{((SoundID)id).ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"For more information, please go to the [Comb Filtering] section in Tools/BroAudio/Preference.");
                }
#endif
                return false;
            }
            return true;
        }

        private bool HasPassPreventionTime(int previousPlayTime)
        {
            int time = TimeExtension.UnscaledCurrentFrameBeganTime;
            bool isInQueue = previousPlayTime == 0f;
            return !isInQueue && time - previousPlayTime >= TimeExtension.SecToMs(Setting.CombFilteringPreventionInSeconds);
        }
    }
}

