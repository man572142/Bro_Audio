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
        private Dictionary<int, MusicPlayer> _musicPlayers = new Dictionary<int, MusicPlayer>();
        private int _latestMusicID = 0;

		public IAudioPlayer PlayMusic(int id,Transition transition,float fadeTime ,float preventTime)
        {
            if (!IsPlayable(id, _musicBank))
            {
                return null;
            }

            MusicPlayer latestPlayer = null;
            MusicPlayer newPlayer = null;
            if (_musicPlayers.TryGetValue(_latestMusicID, out latestPlayer) && _latestMusicID == id)
            {               
                if (latestPlayer.IsPlayingVirtually)
                {
                    // latestPlayer unmute
                }
                else
                {

                    newPlayer = latestPlayer;
                }
            }

            if (newPlayer == null && !TryGetPlayerWithType<MusicPlayer>(out newPlayer))
            {
                return null;
            }

            _latestMusicID = id;
            _musicPlayers[_latestMusicID] = newPlayer;

            switch (transition)
            {
                case Transition.Immediate:
                case Transition.OnlyFadeIn:
                    StopLatest(0f, null);
                    PlayNew();
                    break;
                case Transition.Default:
                case Transition.OnlyFadeOut:
                    StopLatest(fadeTime, () => PlayNew());
                    break;
                case Transition.CrossFade:
                    StopLatest(fadeTime, null);
                    PlayNew();
                    break;
            }

            StartCoroutine(PreventCombFiltering(id, preventTime));
            return newPlayer;

            

            void PlayNew()
            {
                if(_latestMusicID == id)
                {
                    PlaybackPreference pref = new PlaybackPreference(_musicBank[id].Loop);
                    pref.SetFadeTime(transition, fadeTime);

                    newPlayer.Play(id, _musicBank[id].Clip, pref);
                    newPlayer.OnRecycle += OnRecyclePlayer;
                }
            }

            void StopLatest(float fadeOut, Action onFinishStopping)
            {               
                if(latestPlayer != null)
                {
                    latestPlayer.Stop(fadeOut, onFinishStopping);
                }
                else
                {
                    onFinishStopping?.Invoke();
                }
            }

            void OnRecyclePlayer(AudioPlayer recyclePlayer)
            {
                recyclePlayer.OnRecycle -= OnRecyclePlayer;
                _musicPlayers.Remove(recyclePlayer.ID);
            }
        }

        #region PlayMusic Overloads
        public IAudioPlayer PlayMusic(int id) => PlayMusic(id, Transition.Default);
        public IAudioPlayer PlayMusic(int id, Transition transition) => PlayMusic(id, transition, AudioPlayer.UseClipFadeSetting);
        public IAudioPlayer PlayMusic(int id, Transition transition, float fadeTime) => PlayMusic(id, transition, fadeTime, AudioExtension.HaasEffectInSeconds);
        #endregion

        private bool TryGetMusicPlayer(int id, out MusicPlayer player)
        {
            if (_musicPlayers.TryGetValue(id, out player) && player != null && !player.IsBaseNull)
            {
                if (player.IsPlayingVirtually)
                {
                    // UnMute
                }
                else
                {
                    // 可能正在播，也可能沒有，不論如何，就是給他播下去!!
                }
                return true;
            }

            if (TryGetPlayerWithType<MusicPlayer>(out player))
            {
                _musicPlayers[id] = player;
                return true;
            }

            LogError("Can't get MusicPlayer");
            return false;
        }
    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git

