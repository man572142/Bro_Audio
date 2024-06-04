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
		private Queue<Action> _playbackQueue = new Queue<Action>();

        #region Play
        public IAudioPlayer Play(int id)
        {
            if (IsPlayable(id,out var entity, out var previousPlayer) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity);
                return PlayerToPlay(id, player, pref, previousPlayer);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Vector3 position)
        {
            if (IsPlayable(id,out var entity, out var previousPlayer) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity,position);
                return PlayerToPlay(id, player,pref, previousPlayer);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Transform followTarget)
        {
            if (IsPlayable(id,out var entity, out var previousPlayer) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity,followTarget);
                return PlayerToPlay(id, player,pref, previousPlayer);
            }
            return null;
        }

		private IAudioPlayer PlayerToPlay(int id, AudioPlayer player, PlaybackPreference pref, AudioPlayer previousPlayer = null)
        {
            BroAudioType audioType = GetAudioType(id);
            if (_auidoTypePref.TryGetValue(audioType, out var audioTypePref))
            {
                pref.AudioTypePlaybackPref = audioTypePref;
            }

			_playbackQueue.Enqueue(() => player.Play(id, pref));
            var wrapper = new AudioPlayerInstanceWrapper(player);

            if (Setting.AlwaysPlayMusicAsBGM && audioType == BroAudioType.Music)
            {
                player.AsBGM().SetTransition(Setting.DefaultBGMTransition, Setting.DefaultBGMTransitionTime);
            }

            if(CombFilteringPreventionInSeconds > 0f)
            {
                _combFilteringPreventer = _combFilteringPreventer ?? new Dictionary<SoundID, AudioPlayer>();
                if(previousPlayer != null)
                {
                    previousPlayer.OnEndPlaying -= RemoveFromPreventer;
                }
                player.OnEndPlaying += RemoveFromPreventer;
                _combFilteringPreventer[id] = player;
            }

            if (pref.Entity.SeamlessLoop)
            {
                var seamlessLoopHelper = new SeamlessLoopHelper(wrapper, GetNewAudioPlayer);
                seamlessLoopHelper.SetPlayer(player);
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
				_playbackQueue.Dequeue()?.Invoke();
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
            StopPlayer(fadeTime,x => x.ID == id);
        }

        public void Stop(BroAudioType targetType,float fadeTime)
		{
            StopPlayer(fadeTime, x => targetType.Contains(GetAudioType(x.ID)));
        }            

        private void StopPlayer(float fadeTime,Predicate<AudioPlayer> predicate)
        {
            var players = _audioPlayerPool.GetCurrentAudioPlayers();
            if(players != null)
            {
                for (int i = players.Count - 1; i >= 0; i--)
                {
                    var player = players[i];
                    if (player.IsActive && predicate.Invoke(player))
                    {
                        player.Stop(fadeTime);
                    }
                }
			}
        }
        #endregion

        public void Pause(int id)
		{
            Pause(id, AudioPlayer.UseEntitySetting);
		}

        public void Pause(int id,float fadeTime)
		{
            GetCurrentActivePlayers((player) =>
            {
                if (player.ID == id)
                {
                    player.Stop(fadeTime, StopMode.Pause,null);
                }
            });
        }

        private bool IsPlayable(int id, out IAudioEntity entity, out AudioPlayer previousPlayer)
        {
            entity = null;
            previousPlayer = null;
            if (id <= 0 || !_audioBank.TryGetValue(id, out entity))
            {
                Debug.LogError(LogTitle + $"The sound is missing or it has never been assigned. No sound will be played. SoundID:{id}");
                return false;
            }

            SoundID soundID = id;
            if (_combFilteringPreventer != null && _combFilteringPreventer.TryGetValue(soundID, out previousPlayer) 
                && !HasPassPreventionTime(previousPlayer.PlaybackStartingTime))
            {
#if UNITY_EDITOR
                if (Setting.LogCombFilteringWarning)
                {
                    Debug.LogWarning(LogTitle + $"One of the plays of Audio:{soundID.ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"For more information, please go to the [Comb Filtering] section in Tools/BroAudio/Preference.");
                }
#endif
                return false;
            }

            return true;
        }

        private bool HasPassPreventionTime(int ms)
        {
            int time = TimeExtension.UnscaledCurrentFrameBeganTime;
            return time - ms >= TimeExtension.SecToMs(Setting.CombFilteringPreventionInSeconds);
        }
    }
}

