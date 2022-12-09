using MiProduction.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Library;
using System;
using UnityEngine.Profiling;
using static MiProduction.BroAudio.Utility;

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
                LogError($"Initialize failed ,please check {nameof(SoundManager)}.prefab in your Resources folder!");
                return;
            }

            if (prefab.TryGetComponent(out SoundManager soundSystem))
            {
                Instance = soundSystem;
            }
            else
            {
                LogError($"Initialize failed ,please add {nameof(SoundManager)} component to {nameof(SoundManager)}.prefab");
            }

            DontDestroyOnLoad(prefab);
        }

        public static SoundManager Instance = null;

        [Header("Player")]
        [SerializeField] SoundPlayer _sfxPlayer = null;
        [SerializeField] SoundPlayer _uiPlayer = null;
        [SerializeField] SoundPlayer _voicePlayer = null;
        [SerializeField] SoundPlayer _ambiencePlayer = null;
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
                LogError($"Please add at least 2 MusicPlayer to {nameof(SoundManager)}");
            }

            InitSoundBank();
            InitRandomSoundBank();
            InitMusicBank();
        }

		private void InitSoundBank()
		{
            foreach (var soundAsset in _allSoundAssets)
            {
                if(soundAsset == null)
				{
                    continue;
				}
                for (int s = 0; s < soundAsset.Libraries.Length; s++)
                {
                    var soundLibrary = soundAsset.Libraries[s];
                    if (_soundBank.ContainsKey(soundLibrary.ID))
                    {
                        LogError($"Sound :{soundLibrary.EnumName} is duplicated !");
                        return;
                    }
                    if (soundLibrary.Validate(s))
                    {
                        _soundBank.Add(soundLibrary.ID, soundLibrary);
                    }
                }
            }
            if(_soundBank.Count == 0)
			{
                LogError($"There isn't any sound asset in the {nameof(SoundManager)}!");
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
            if(_mainMusicAsset == null)
			{
                LogError($"There isn't any music asset in the {nameof(SoundManager)}!");
                return;
			}

            for (int m = 0; m < _mainMusicAsset.Libraries.Length; m++)
            {

                var musicLibrary = _mainMusicAsset.Libraries[m];
                if (_musicBank.ContainsKey(musicLibrary.ID))
                {
                    LogError($"Music :{musicLibrary.EnumName} is duplicated !");
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
                        LogError("No playable music player for another music!");
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

        public void PlaySound(int id, float preventTime)
        {
            if(GetSoundPlayer(id,out SoundPlayer soundPlayer))
            {
                soundPlayer.Play(id, _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
            }     
        }

        public void PlaySound(int id, Vector3 position)
        {
            if(GetSoundPlayer(id, out SoundPlayer soundPlayer))
            {
                soundPlayer.PlayAtPoint( _soundBank[id].Clip, _soundBank[id].Delay, _soundBank[id].Volume, position);
            }
        }

        public void PlayRandomSFX(int id, float preventTime)
        {
            if(RandomSoundCheck(id))
            {
                _sfxPlayer.Play(id, GetRandomClip(id), _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
            }      
        }

        public void SetSoundVolume(float vol,float fadeTime)
		{
            _sfxPlayer.SetVolume(vol,fadeTime);
		}

        public void StopSound(float fadeTime)
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
            if (id == 0)
            {
                LogError("Music ID is 0 (None). No music will play");
                return false;
            }
            else if (!_musicBank.ContainsKey(id))
            {
                LogError($"Music ID: {id} may not exist ,please check the MusicAsset's setting");
                return false;
            }
            else if(id == _currentPlayer.CurrentMusicID)
            {
                LogWarning("The music you want to play is already playing");
                return false;
            }
            return true;
        }

        private bool GetSoundPlayer(int id,out SoundPlayer player)
        {
            player = null;
            AudioType audioType = Utility.GetAudioType(id);

			switch (audioType)
			{
				case AudioType.UI:
                    player = _uiPlayer;
					break;
				case AudioType.SFX:
                    player = _sfxPlayer;
					break;
				case AudioType.VoiceOver:
                    player = _voicePlayer;
					break;
                case AudioType.Ambience:
                    player = _ambiencePlayer;
                    break;
                default:
                    LogWarning($"{audioType} is not suppose to play in any SoundPlayer");
                    return false;
			}

            if(player == null)
			{
                LogError($"The SoundPlayer for {audioType} has null refernece");
			}
			if (player == null || _soundBank.Count < 1)
            {
                LogError("No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_soundBank.ContainsKey(id))
            {
                LogError($"Audio may not exist in the current SoundAsset,AudioType:{audioType},SoundID:{id}");
                return false;
            }

            return true;
		}

		private bool RandomSoundCheck(int id)
        {
            if (_sfxPlayer == null || _randomSoundAsset == null || _randomSoundBank.Count < 1 || !_randomSoundBank.ContainsKey(id))
            {
                LogError("No sound can play , please check SoundManager's setting");
                return false;
            }
            else if (!_randomSoundBank.ContainsKey((int)id))
            {
                LogError($"SoundID:{id} may not exist in the current RandomSoundAsset");
                return false;
            }
            return true;
        }
		#endregion
	}

}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git

