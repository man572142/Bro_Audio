using System;
using UnityEngine;
using static Ami.BroAudio.Utility;
using System.Collections.Generic;

namespace Ami.BroAudio.Runtime
{
	public partial class SoundManager : MonoBehaviour
    {
        public Dictionary<int, AudioPlayer> ResumablePlayers = null;

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
                var pref = new PlaybackPreference(entity,position);
                return PlayerToPlay(id, player,pref);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Transform followTarget)
        {
            if (IsPlayable(id,out var entity) && TryGetAvailablePlayer(id, out var player))
            {
                var pref = new PlaybackPreference(entity,followTarget);
                return PlayerToPlay(id, player,pref);
            }
            return null;
        }

		private IAudioPlayer PlayerToPlay(int id, AudioPlayer player,PlaybackPreference pref)
        {
            BroAudioType audioType = GetAudioType(id);
            if (_auidoTypePref.TryGetValue(audioType, out var audioTypePref))
            {
                pref.AudioTypePlaybackPref = audioTypePref;
            }

            player.Play(id, pref);
            var wrapper = new AudioPlayerInstanceWrapper(player);

            if (Setting.AlwaysPlayMusicAsBGM && audioType == BroAudioType.Music)
            {
                player.AsBGM().SetTransition(Setting.DefaultBGMTransition, Setting.DefaultBGMTransitionTime);
            }

            if(CombFilteringPreventionInSeconds > 0f)
            {
				StartCoroutine(PreventCombFiltering(id, CombFilteringPreventionInSeconds));
			}

            if (pref.Entity.SeamlessLoop)
            {
                var seamlessLoopHelper = new SeamlessLoopHelper(wrapper, GetNewAudioPlayer);
                seamlessLoopHelper.SetPlayer(player);
            }
            return wrapper;
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
    }
}

