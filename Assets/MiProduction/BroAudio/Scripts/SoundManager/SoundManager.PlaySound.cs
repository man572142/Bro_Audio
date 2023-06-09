using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using System;

namespace MiProduction.BroAudio.Runtime
{
    public partial class SoundManager : MonoBehaviour
    {
        public IAudioPlayer Play(int id, float preventTime)
        {
            BroAudioType audioType = GetAudioType(id);
            if (audioType == BroAudioType.Music || audioType == BroAudioType.Ambience)
            {
                return PlayMusic(id, Transition.Immediate, AudioPlayer.UseClipFadeSetting, preventTime);
            }

            if (IsPlayable(id, _soundBank) && TryGetPlayer(out var player))
            {
                PlaybackPreference setting = new PlaybackPreference(_soundBank[id].Delay);
                player.Play(id, _soundBank[id].Clip, setting);
                StartCoroutine(PreventCombFiltering(id, preventTime));
                return player;
            }
            return null;
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
    }
}

// by «} 2022
// https://github.com/man572142/Bro_Audio.git

