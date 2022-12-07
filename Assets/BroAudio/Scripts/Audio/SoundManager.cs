using MiProduction.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Library;
using System;
using UnityEngine.Profiling;

namespace MiProduction.BroAudio.Core
{
    [DisallowMultipleComponent]
    public class SoundManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            GameObject prefab = Instantiate(Resources.Load("SoundManager")) as GameObject;

            if (prefab == null)
            {
                Debug.LogError("[SoundSystem] initialize failed ,please check SoundManager.prefab in your Resources folder!");
                return;
            }

            if (prefab.TryGetComponent(out SoundManager soundSystem))
            {
                Instance = soundSystem;
            }
            else
            {
                Debug.LogError("[SoundSystem] initialize failed ,please add SoundManager component to SoundManager.prefab");
            }

            DontDestroyOnLoad(prefab);
        }

        public static SoundManager Instance = null;

        [Header("Player")]
        [SerializeField] SoundPlayer _sfxPlayer = null;
        [SerializeField] SoundPlayer _uiPlayer = null;
        [SerializeField] SoundPlayer _standOutPlayer = null;
        [SerializeField] MusicPlayer[] _musicPlayers = null;
        private MusicPlayer _currentPlayer;

        [Header("Fading Setting")]
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;
        

        // 音效
        [Header("Library")]
        [SerializeField] SoundLibraryAsset[] _allSoundAssets = null;
        private Dictionary<int, SoundLibrary> _soundBank = new Dictionary<int, SoundLibrary>();

        // 隨機播放音效
        [SerializeField] SoundLibraryAsset[] _randomSoundAsset = null;
        Dictionary<int, SoundLibrary[]> _randomSoundBank = new Dictionary<int, SoundLibrary[]>();

        // 音樂
        [SerializeField] MusicLibraryAsset _mainMusicAsset = null;
        private Dictionary<int, MusicLibrary> _musicBank = new Dictionary<int, MusicLibrary>();


        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }


        private void Awake()
        {
            if (_musicPlayers == null || _musicPlayers.Length < 2)
            {
                Debug.LogError("[SoundSystem] Please add at least 2 MusicPlayer to SoundManager");
            }

            InitSoundBank();
            InitRandomSoundBank();
            InitMusicBank();
        }

		private void InitSoundBank()
		{
            foreach (var soundAsset in _allSoundAssets)
            {
                for (int s = 0; s < soundAsset.Libraries.Length; s++)
                {
                    var soundLibrary = soundAsset.Libraries[s];
                    if (_soundBank.ContainsKey(soundLibrary.ID))
                    {
                        Debug.LogError($"[SoundSystem] Sound :{soundLibrary.EnumName} is duplicated !");
                        return;
                    }
                    if (soundLibrary.Validate(s))
                    {
                        _soundBank.Add(soundLibrary.ID, soundLibrary);
                    }
                }
            }
        }
        private void InitRandomSoundBank()
        {
            bool isValidated;
            foreach (SoundLibraryAsset asset in _randomSoundAsset)
            {
                isValidated = true;
                for (int r = 0; r < asset.Libraries.Length; r++)
                {
                    if (!asset.Libraries[r].Validate(r))
                    {
                        isValidated = false;
                        break;
                    }
                }
                if (isValidated)
                    _randomSoundBank.Add(asset.Libraries[0].ID, asset.Libraries);
            }
        }
        private void InitMusicBank()
        {
            for (int m = 0; m < _mainMusicAsset.Libraries.Length; m++)
            {
                var musicLibrary = _mainMusicAsset.Libraries[m];
                if (_musicBank.ContainsKey(musicLibrary.ID))
                {
                    Debug.LogError($"[SoundSystem] Music :{musicLibrary.EnumName} is duplicated !");
                    return;
                }
                if (musicLibrary.Validate(m))
                {
                    _musicBank.Add(musicLibrary.ID, musicLibrary);
                }
            }
            _currentPlayer = _musicPlayers[0];
        }


        #region 音樂

        public void PlayMusic(int id,Transition transition,float fadeTime = -1)
        {
            if (!PlayMusicCheck(id))
                return;

            switch (transition)
            {
                case Transition.Immediate:
                    _currentPlayer.Stop(0f);
                    _currentPlayer.Play(_musicBank[id], 0f);
                    break;
                case Transition.FadeOutThenFadeIn:
                    _currentPlayer.Stop(fadeTime, () => _currentPlayer.Play(_musicBank[id], fadeTime));
                    break;
                case Transition.OnlyFadeInNew:
                    _currentPlayer.Stop(0f);
                    _currentPlayer.Play(_musicBank[id], fadeTime);
                    break;
                case Transition.OnlyFadeOutCurrent:
                    _currentPlayer.Stop(fadeTime, () => _currentPlayer.Play(_musicBank[id], 0f));
                    break;
                case Transition.CrossFade:
                    if (GetAvailableMusicPlayer(out MusicPlayer otherPlayer))
                    {
                        _currentPlayer.Stop(fadeTime, () => _currentPlayer = otherPlayer);
                        otherPlayer.Play(_musicBank[id], fadeTime);
                    }
                    else
                    {
                        Debug.LogError("[SoundSystem] No playable music player for another music!");
                    }
                    break;
            }
        }

        private bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer)
        {
            musicPlayer = null;
            foreach (MusicPlayer player in _musicPlayers)
            {
                if (!player.IsPlaying && player != _currentPlayer)
                {
                    musicPlayer = player;
                    return true;
                }
            }
            return false;
        }

        public void StopMusic(float fadeTime)
		{
            _currentPlayer.Stop(fadeTime);
		}

        public void SetMusicVolume(float vol,float fadeTime)
		{
            _currentPlayer.SetVolume(vol,fadeTime);
		}


        #endregion

        #region 音效

        public void PlaySFX(int id, float preventTime)
        {
            if(SoundPlayerCheck(id))
            {
                _sfxPlayer.Play(id, _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
            }     
        }

        public void PlaySFX(int id, Vector3 position)
        {
            if(SoundPlayerCheck(id))
            {
                _sfxPlayer.PlayAtPoint( _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, position);
            }
        }

        public void PlayRandomSFX(int id, float preventTime)
        {
            if(RandomSoundCheck(id))
            {
                _sfxPlayer.Play(id, GetRandomClip(id), _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
            }      
        }

        public void SetSFXVolume(float vol,float fadeTime)
		{
            _sfxPlayer.SetVolume(vol,fadeTime);
		}

        public void StopSFX(float fadeTime)
		{
            _sfxPlayer.Stop(fadeTime);
		}

        private AudioClip GetRandomClip(int id)
        {
            int index = UnityEngine.Random.Range(0, _randomSoundBank[id].Length);
            return _randomSoundBank[id][index].Clip;
        }

        #endregion


        #region NullChecker
        private bool PlayMusicCheck(int id)
        {
#if UNITY_EDITOR
            if (_mainMusicAsset == null || _musicBank.Count < 1 || id == 0)
            {
                Debug.LogError("[SoundSystem] No music can play. please check SoundManager's setting!");
                return false;
            }
            else if (!_musicBank.ContainsKey(id))
            {
                Debug.LogError($"[SoundSystem] Music ID: {id} may not exist in the current MusicAsset");
                return false;
            }
            else if(id == _currentPlayer.CurrentMusicID)
            {
                Debug.LogWarning("[SoundSystem] The music you want to play is already playing");
                return false;
            }
#endif
            return true;
        }

        private bool SoundPlayerCheck(int id)
        {
#if UNITY_EDITOR
            if (_sfxPlayer == null || _allSoundAssets == null || _soundBank.Count < 1)
            {
                Debug.LogError("[SoundSystem] No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_soundBank.ContainsKey(id))
            {
                Debug.LogError($"[SoundSystem] SoundID:{id} may not exist in the current SoundAsset");
                return false;
            }
#endif
            return true;
        }

        private bool RandomSoundCheck(int id)
        {
#if UNITY_EDITOR
            if (_sfxPlayer == null || _randomSoundAsset == null || _randomSoundBank.Count < 1 || !_randomSoundBank.ContainsKey(id))
            {
                Debug.LogError("[SoundSystem] No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_randomSoundBank.ContainsKey((int)id))
            {
                Debug.LogError($"[SoundSystem] SoundID:{id} may not exist in the current RandomSoundAsset");
                return false;
            }
#endif
            return true;
        }

        #endregion

    }

}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git

