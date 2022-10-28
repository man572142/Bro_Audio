using MiProduction.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Library;

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
        [SerializeField] SoundPlayer _sfxPlayer;
        [SerializeField] MusicPlayer[] _musicPlayers = null;

        [Header("Fading Setting")]
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;
        MusicPlayer _currentPlayer;


        // TODO: LibraryAsset陣列化
        // 音效
        [Header("Library")]
        [SerializeField] SoundLibraryAsset _mainSoundAsset = null;
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
            // 初始化音效庫
            for (int s = 0; s < _mainSoundAsset.Libraries.Length; s++)
            {
                if(_soundBank.ContainsKey(_mainSoundAsset.Libraries[s].ID))
                {
                    LogDuplicateError("Sound", _mainSoundAsset.Libraries[s].ID.ToString());
                    return;
                }
                if (_mainSoundAsset.Libraries[s].Validate(s))
                {
                    _soundBank.Add(_mainSoundAsset.Libraries[s].ID, _mainSoundAsset.Libraries[s]);
                }
            }
            // 初始化隨機播放音效庫
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
                if(isValidated)
                    _randomSoundBank.Add(asset.Libraries[0].ID, asset.Libraries);
            }
            // 初始化音樂庫
            for (int m = 0; m < _mainMusicAsset.Libraries.Length; m++)
            {
                if (_musicBank.ContainsKey(_mainMusicAsset.Libraries[m].ID))
                {
                    LogDuplicateError("Music", _mainMusicAsset.Libraries[m].ID.ToString());
                    return;
                }
                if (_mainMusicAsset.Libraries[m].Validate(m) && !_musicBank.ContainsKey(_mainMusicAsset.Libraries[m].ID))
                {
                    _musicBank.Add(_mainMusicAsset.Libraries[m].ID, _mainMusicAsset.Libraries[m]);
                }
            }
            _currentPlayer = _musicPlayers[0];
        }


        #region 音樂

        public void PlayMusic(Music newMusic)
        {
            PlayMusic(newMusic, Transition.Immediate);
        }

        public void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f)
        {
            if (!PlayMusicCheck(newMusic))
                return;

            int id = (int)newMusic;
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

        public bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer)
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

        public void PlaySFX(Sound sound)
        {
            PlaySFX(sound, 0.1f);
        }

        public void PlaySFX(Sound sound, float preventTime)
        {
            int id = (int)sound;
            if(SoundPlayerCheck(sound))
            {
                _sfxPlayer.Play(sound, _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
            }     
        }

        public void PlaySFX(Sound sound, Vector3 position)
        {
            if(SoundPlayerCheck(sound))
            {
                int id = (int)sound;
                _sfxPlayer.PlayAtPoint(sound, _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, position);
            }
        }

        public void PlayRandomSFX(Sound sound)
        {
            PlayRandomSFX(sound, 0.1f);
        }

        public void PlayRandomSFX(Sound sound, float preventTime)
        {
            if(RandomSoundCheck(sound))
            {
                int id = (int)sound;
                _sfxPlayer.Play(sound, GetRandomClip(sound), _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
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

        private AudioClip GetRandomClip(Sound sound)
        {
            int id = (int)sound;
            int index = Random.Range(0, _randomSoundBank[id].Length);
            return _randomSoundBank[id][index].Clip;
        }

        #endregion


        #region NullChecker
        private bool PlayMusicCheck(Music music)
        {
#if UNITY_EDITOR
            if (_mainMusicAsset == null || _musicBank.Count < 1 || music == Music.None)
            {
                Debug.LogError("[SoundSystem] No music can play. please check SoundManager's setting!");
                return false;
            }
            else if (!_musicBank.ContainsKey((int)music))
            {
                Debug.LogError($"[SoundSystem] Enum:{music.ToString()} may not exist in the current MusicAsset");
                return false;
            }
            else if(music == _currentPlayer.CurrentMusic)
            {
                Debug.LogWarning("[SoundSystem] The music you want to play is already playing");
                return false;
            }
#endif
            return true;
        }

        private bool SoundPlayerCheck(Sound sound)
        {
#if UNITY_EDITOR
            if (_sfxPlayer == null || _mainSoundAsset == null || _soundBank.Count < 1)
            {
                Debug.LogError("[SoundSystem] No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_soundBank.ContainsKey((int)sound))
            {
                Debug.LogError($"[SoundSystem] Enum:{sound.ToString()} may not exist in the current SoundAsset");
                return false;
            }
#endif
            return true;
        }

        private bool RandomSoundCheck(Sound sound)
        {
#if UNITY_EDITOR
            if (_sfxPlayer == null || _randomSoundAsset == null || _randomSoundBank.Count < 1 || !_randomSoundBank.ContainsKey((int)sound))
            {
                Debug.LogError("[SoundSystem] No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_randomSoundBank.ContainsKey((int)sound))
            {
                Debug.LogError($"[SoundSystem] Enum:{sound.ToString()} may not exist in the current RandomSoundAsset");
                return false;
            }
#endif
            return true;
        }

        #endregion

        private void LogDuplicateError(string enumType,string enumName)
        {
            Debug.LogError($"[SoundSystem] Enum {enumType}.{enumName} is duplicated !");
        }
    }

}



