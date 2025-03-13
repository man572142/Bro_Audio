using System.Collections.Generic;
using UnityEngine;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Utility;
using System;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        private Queue<IPlayable> _playbackQueue = new Queue<IPlayable>();
        private Action<SoundID> _removeFromPreventerDelegate;

        #region Play
        public IAudioPlayer Play(SoundID id, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Vector3 position, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, position);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        public IAudioPlayer Play(SoundID id, Transform followTarget, IPlayableValidator customValidator = null)
        {
            if (IsPlayable(id, customValidator, out var entity, out var player))
            {
                var pref = new PlaybackPreference(entity, followTarget);
                return PlayerToPlay(id, player, pref);
            }
            return Empty.AudioPlayer;
        }

        private bool IsPlayable(SoundID id, IPlayableValidator customValidator, out IAudioEntity entity, out AudioPlayer player)
        {
            entity = null;
            player = null;

            if(id <= 0 || !_audioBank.TryGetValue(id, out entity))
            {
#if UNITY_EDITOR
                Debug.LogError(LogTitle + $"The sound is missing or it has never been assigned. No sound will be played. Object:{id.DebugObject?.name}", id.DebugObject); 
#endif
                return false;
            }

            var validator = customValidator ?? entity.PlaybackGroup; // entity's runtime group will be set in InitBank() if it's null

            bool isValid = validator == null || validator.IsPlayable(id);
            bool result = isValid && TryGetAvailablePlayer(id, out player);
            if(validator != null && player != null)
            {
                validator.OnGetPlayer(player);
            }
            return result;
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

            // Whether there's any group implementing this or not, we're tracking it anyway
            _combFilteringPreventer ??= new Dictionary<SoundID, AudioPlayer>();
            _combFilteringPreventer[id] = player;
            _removeFromPreventerDelegate ??= RemoveFromPreventer;
            player.OnEnd(_removeFromPreventerDelegate);

            if (pref.Entity.SeamlessLoop)
            {
                var seamlessLoopHelper = new SeamlessLoopHelper(wrapper, GetNewAudioPlayer);
                seamlessLoopHelper.AddReplayListener(player);
            }

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
            foreach (var player in GetCurrentAudioPlayers())
            {
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
            foreach (var player in GetCurrentAudioPlayers())
            {
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

        public bool HasPassCombFilteringPreventionTime(SoundID id, float combFilteringTime, bool ignoreCombFilteringIfSameFrame)
        {
            if(combFilteringTime <= 0f)
            {
                return true;
            }

            if (_combFilteringPreventer != null && _combFilteringPreventer.TryGetValue(id, out var previousPlayer))
            {
                int time = TimeExtension.UnscaledCurrentFrameBeganTime;
                int previousPlayTime = previousPlayer.PlaybackStartingTime;
                // the previous has been added to the queue but hasn't played yet, i.e. The current and the previous will end up being played in the same frame
                bool previousIsInQueue = Mathf.Approximately(previousPlayTime, 0f); 
                float difference = time - previousPlayTime;
                if(previousIsInQueue || Mathf.Approximately(difference, 0f))
                {
                    return ignoreCombFilteringIfSameFrame;
                }

                return difference >= TimeExtension.SecToMs(combFilteringTime);
            }
            return true;
        }
    }
}