using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Data;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
		#region Play
		public IAudioPlayer Play(int id, float preventTime)
        {
            BroAudioType audioType = GetAudioType(id);
            bool isPersistentType = PersistentType.HasFlag(audioType);

            if (IsPlayable(id) && TryGetPlayer(id,out var player))
            {
                var lib = _audioBank[id];
                var pref = isPersistentType ? new PlaybackPreference(lib.CastTo<MusicLibrary>().Loop, 0f)
                    : new PlaybackPreference(false, lib.CastTo<SoundLibrary>().Delay);

                player.Play(id, lib.CastTo<AudioLibrary>().Clip, pref);

                StartCoroutine(PreventCombFiltering(id, preventTime));
                return player as IAudioPlayer;
            }

            return null;

            bool TryGetPlayer(int id,out IPlaybackControllable audioPlayer)
			{
                audioPlayer = null;
                if (AudioPlayer.ResumablePlayers == null || !AudioPlayer.ResumablePlayers.TryGetValue(id,out audioPlayer))
				{
                    if (TryGetNewAudioPlayer(out AudioPlayer newPlayer))
                    {
                        audioPlayer = newPlayer;
                    }
                }
                
                return audioPlayer != null;
            }
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

		//      public IAudioPlayer PlayAtPoint(int id, Vector3 position, float preventTime)
		//      {
		//          if(IsPlayable(id,_soundBank) && TryGetPlayerWithType<OneShotPlayer>(out var player))
		//          {
		//              player.PlayAtPoint(id,_soundBank[id].Clip, _soundBank[id].Delay, position);
		//              StartCoroutine(PreventCombFiltering(id,preventTime));
		//              return player;
		//          }
		//          return null;
		//      }

		#endregion

		#region Stop
		public void StopPlaying(BroAudioType audioType)
        {
            if (audioType == BroAudioType.All)
            {
                LoopAllAudioType((loopAudioType) => Stop(loopAudioType));
            }
            else
            {
                Stop(audioType);
            }

            void Stop(BroAudioType target)
            {
                if (_audioPlayerPool.TryGetObject(x => target.HasFlag(GetAudioType(x.ID)), out var player))
                {
                    player.Stop();
                }
            }
        }

        public void StopPlaying(int id)
        {
            if (_audioPlayerPool.TryGetObject(x => x.ID == id, out var player))
            {
                player.Stop();
            }
        }
        #endregion
    }
}

