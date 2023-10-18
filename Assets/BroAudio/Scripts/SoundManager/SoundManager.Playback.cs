using System;
using UnityEngine;
using Ami.BroAudio.Data;
using static Ami.BroAudio.Utility;
using System.Collections.Generic;
using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        public class AudioTypePlaybackPreference
		{
            //TODO: 跟另一個PlaybackPref整合?
            public float Volume = AudioConstant.FullVolume;
            public EffectType EffectType = EffectType.None;
		}

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
                player.SetEffect(audioTypePref.EffectType, SetEffectMode.Override);
                player.SetVolume(audioTypePref.Volume, 0f);
            }

            var entity = _audioBank[id];
            var clip = entity.Clip;
            player.Play(id, clip, pref);

            AudioPlayerInstanceWrapper wrapper = new AudioPlayerInstanceWrapper(player);

            if (pref.IsSeamlessLoop)
            {
                var seamlessLoopHelper = new SeamlessLoopHelper(wrapper, GetNewAudioPlayer);
                seamlessLoopHelper.SetPlayer(player);
            }

            StartCoroutine(PreventCombFiltering(id, HaasEffectInSeconds));

            return wrapper;
        }


        //public IAudioPlayer PlayOneShot(int id, float preventTime)
        //      {
        //          if(IsPlayable(id,_soundBank)&& TryGetPlayerWithType<OneShotPlayer>(out var player))
        //          {
        //              player.PlayOneShot(id, _soundBank[id].Clip, _soundBank[id].Delay);
        //              StartCoroutine(PreventCombFiltering(id,preventTime));
        //              return player;
        //          }
        //          return null;
        //      }

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
            StopPlayer(fadeTime, x => targetType.HasFlag(GetAudioType(x.ID)));
        }            

        private void StopPlayer(float fadeTime,Predicate<AudioPlayer> predicate)
        {
            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach (var player in players)
            {
                if (predicate.Invoke(player))
                {
                    player.Stop(fadeTime);
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
            GetCurrentInUsePlayers((player) =>
            {
                if (player.ID == id)
                {
                    player.Stop(fadeTime, StopMode.Pause,null);
                }
            });
        }
    }
}

