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
        private Dictionary<Sound, SoundLibrary> _soundBank = new Dictionary<Sound, SoundLibrary>();
        private Dictionary<Sound, bool> _preventPlayback = new Dictionary<Sound, bool>();

        // 隨機播放音效
        [SerializeField] SoundLibraryAsset[] _randomSoundAsset = null;
        Dictionary<Sound, SoundLibrary[]> _randomSoundBank = new Dictionary<Sound, SoundLibrary[]>();

        // 音樂
        [SerializeField] MusicLibraryAsset _mainMusicAsset = null;
        private Dictionary<Music, MusicLibrary> _musicBank = new Dictionary<Music, MusicLibrary>();


        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }


        private void Awake()
        {
            if (_musicPlayers.Length < 2)
            {
                Debug.LogError("[SoundSystem] Please add at least 2 MusicPlayer to SoundManager");
            }
            // 初始化音效庫
            for (int s = 0; s < _mainSoundAsset.Libraries.Length; s++)
            {
                if (_mainSoundAsset.Libraries[s].Validate(s))
                {
                    _soundBank.Add(_mainSoundAsset.Libraries[s].Sound, _mainSoundAsset.Libraries[s]);
                    _preventPlayback.Add(_mainSoundAsset.Libraries[s].Sound, false);
                }
            }
            // 初始化隨機播放音效庫
            foreach (SoundLibraryAsset asset in _randomSoundAsset)
            {
                for (int r = 0; r < asset.Libraries.Length; r++)
                {
                    if (!asset.Libraries[r].Validate(r))
                        break;
                }
                _randomSoundBank.Add(asset.Libraries[0].Sound, asset.Libraries);
                _preventPlayback.Add(asset.Libraries[0].Sound, false);
            }
            // 初始化音樂庫
            for (int m = 0; m < _mainMusicAsset.Libraries.Length; m++)
            {
                if (_mainMusicAsset.Libraries[m].Validate(m))
                {
                    _musicBank.Add(_mainMusicAsset.Libraries[m].Music, _mainMusicAsset.Libraries[m]);
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
            if (_musicBank.Count < 1 || newMusic == Music.None)
            {
                Debug.LogError("[SoundSystem] No music can play!");
                return;
            }

            switch (transition)
            {
                case Transition.Immediate:
                    _currentPlayer.Stop(0f);
                    _currentPlayer.Play(_musicBank[newMusic], 0f);
                    break;
                case Transition.FadeOutThenFadeIn:
                    _currentPlayer.Stop(fadeTime, () => _currentPlayer.Play(_musicBank[newMusic], fadeTime));
                    break;
                case Transition.OnlyFadeInNew:
                    _currentPlayer.Stop(0f);
                    _currentPlayer.Play(_musicBank[newMusic], fadeTime);
                    break;
                case Transition.OnlyFadeOutCurrent:
                    _currentPlayer.Stop(fadeTime, () => _currentPlayer.Play(_musicBank[newMusic], 0f));
                    break;
                case Transition.CrossFade:
                    if (GetAvailableMusicPlayer(out MusicPlayer otherPlayer))
                    {
                        _currentPlayer.Stop(fadeTime, () => _currentPlayer = otherPlayer);
                        otherPlayer.Play(_musicBank[newMusic], fadeTime);
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

        #endregion

        #region 音效

        public void PlaySFX(Sound sound)
        {
            _sfxPlayer.Play(sound, _soundBank[sound].Clip, _soundBank[sound].Delay, _soundBank[sound].Volume, 0.1f);
        }

        public void PlaySFX(Sound sound, float preventTime)
        {
            _sfxPlayer.Play(sound, _soundBank[sound].Clip, _soundBank[sound].Delay, _soundBank[sound].Volume, preventTime);
        }

        public void PlaySFX(Sound sound, Vector3 position)
        {
            _sfxPlayer.PlayAtPoint(sound, _soundBank[sound].Clip, _soundBank[sound].Delay, _soundBank[sound].Volume, position);
        }

        public void PlayRandomSFX(Sound sound)
        {
            _sfxPlayer.Play(sound, GetRandomClip(sound), _soundBank[sound].Delay, _soundBank[sound].Volume, 0.1f);
        }

        public void PlayRandomSFX(Sound sound, float preventTime)
        {
            _sfxPlayer.Play(sound, GetRandomClip(sound), _soundBank[sound].Delay, _soundBank[sound].Volume, preventTime);
        }

        private AudioClip GetRandomClip(Sound sound)
        {
            int index = Random.Range(0, _randomSoundBank[sound].Length);
            return _randomSoundBank[sound][index].Clip;
        }

        #endregion

        IEnumerator PreventPlaybackControl(Sound sound, float time)
        {
            _preventPlayback[sound] = true;
            yield return new WaitForSeconds(time);
            _preventPlayback[sound] = false;
        }

        
    }

}



