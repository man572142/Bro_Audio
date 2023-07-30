using System;
using UnityEngine;
using MiProduction.BroAudio.Data;
using static MiProduction.BroAudio.Utility;
using System.Collections.Generic;
using MiProduction.Extension;

namespace MiProduction.BroAudio.Runtime
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
            if (IsPlayable(id,out var library) && TryGetPlayer(id, out var player))
            {
                var pref = new PlaybackPreference(library);
                return PlayerToPlay(id, player, pref);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Vector3 position)
        {
            if (IsPlayable(id,out var library) && TryGetPlayer(id, out var player))
            {
                var pref = new PlaybackPreference(library,position);
                return PlayerToPlay(id, player,pref);
            }
            return null;
        }

        public IAudioPlayer Play(int id, Transform followTarget)
        {
            if (IsPlayable(id,out var library) && TryGetPlayer(id, out var player))
            {
                var pref = new PlaybackPreference(library,followTarget);
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

            var library = _audioBank[id];
            var clip = library.CastTo<AudioLibrary>().Clip;
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
            ForeachAudioType((audioType) => 
            {
                StopPlayer(x => targetType.HasFlag(GetAudioType(x.ID)));           
            });
        }

        public void Stop(int id)
        {
            StopPlayer(x => x.ID == id);
        }

        private void StopPlayer(Predicate<AudioPlayer> predicate)
        {
            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach (var player in players)
            {
                if (predicate.Invoke(player))
                {
                    player.Stop();
                }
            }
        }
        #endregion
    }
}

