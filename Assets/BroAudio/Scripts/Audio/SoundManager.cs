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
        private MusicPlayer _currMusicPlayer;

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

		#region InitBank
		private void InitSoundBank()
		{
			foreach (var soundAsset in _allSoundAssets)
			{
				if (soundAsset == null)
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
			if (_mainMusicAsset == null)
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
			_currMusicPlayer = _musicPlayers[0];
		} 
		#endregion


		#region 音樂

		public IAudioPlayer PlayMusic(int id,Transition transition,float fadeTime = -1)
        {
            if (!PlayMusicCheck(id))
                return null;

            BroAudioClip clip = _musicBank[id].Clip;
            bool isLoop = _musicBank[id].Loop;

            switch (transition)
            {
                case Transition.Immediate:
                    _currMusicPlayer.Stop(0f);
                    _currMusicPlayer.Play(id,clip,isLoop ,0f);
                    break;
                case Transition.FadeOutThenFadeIn:
                    _currMusicPlayer.Stop(fadeTime, () => _currMusicPlayer.Play(id,clip,isLoop, fadeTime));
                    break;
                case Transition.OnlyFadeInNew:
                    _currMusicPlayer.Stop(0f);
                    _currMusicPlayer.Play(id,clip,isLoop, fadeTime);
                    break;
                case Transition.OnlyFadeOutCurrent:
                    _currMusicPlayer.Stop(fadeTime, () => _currMusicPlayer.Play(id,clip,isLoop, 0f));
                    break;
                case Transition.CrossFade:
                    if (GetAvailableMusicPlayer(out MusicPlayer otherPlayer))
                    {
                        _currMusicPlayer.Stop(fadeTime, () => _currMusicPlayer = otherPlayer);
                        otherPlayer.Play(id,clip,isLoop, fadeTime);
                    }
                    else
                    {
                        LogError("No playable music player for another music!");
                    }
                    break;
            }
            return _currMusicPlayer;
        }

        private bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer)
        {
            musicPlayer = null;
            foreach (MusicPlayer player in _musicPlayers)
            {
                if (!player.IsPlaying && player != _currMusicPlayer)
                {
                    musicPlayer = player;
                    return true;
                }
            }
            return false;
        }

        public void StopMusic(float fadeTime)
		{
            _currMusicPlayer.Stop(fadeTime);
		}

        private void SetMusicVolume(float vol,float fadeTime)
		{
            _currMusicPlayer.SetVolume(vol,fadeTime);
		}


        #endregion

        #region 音效

        public IAudioPlayer PlaySound(int id, float preventTime)
        {
            if(IsInSoundBank(id) && TryGetPlayer(id,out AudioPlayer audioPlayer))
            {
                SoundPlayer soundPlayer = audioPlayer as SoundPlayer;
                soundPlayer?.Play(id, _soundBank[id].Clip, _soundBank[id].Delay, preventTime);
                return soundPlayer;
            }
            return null;
        }

        public IAudioPlayer PlaySound(int id, Vector3 position)
        {
            if(IsInSoundBank(id) && TryGetPlayer(id, out AudioPlayer audioPlayer))
            {
                SoundPlayer soundPlayer = audioPlayer as SoundPlayer;
                soundPlayer?.PlayAtPoint( _soundBank[id].Clip, _soundBank[id].Delay, position);
                return soundPlayer;
            }
            return null;
        }

        //public IAudioPlayer PlayRandomSFX(int id, float preventTime)
        //{
        //    // 待修整
        //    if(RandomSoundCheck(id))
        //    {
        //        _sfxPlayer.Play(id, GetRandomClip(id), _soundBank[id].Delay, _soundBank[id].Volume, preventTime);
        //        return _sfxPlayer;
        //    }
        //    return null;
        //}

        //private AudioClip GetRandomClip(int id)
        //{
        //    int index = UnityEngine.Random.Range(0, _randomSoundBank[id].Length);
        //    return _randomSoundBank[id][index].Clip;
        //}

		private bool TryGetPlayer(int id, out AudioPlayer player)
        {
            AudioType audioType = GetAudioType(id);
            return TryGetPlayer(audioType, out player);
        }

        private bool TryGetPlayer(AudioType audioType, out AudioPlayer player)
        {
            player = null;
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
				case AudioType.Music:
                    player = _currMusicPlayer;
					break;
				default:
					LogWarning($"{audioType} is not suppose to play in any AudioPlayer");
					return false;
			}

			if (player == null)
            {
                LogError($"The AudioPlayer for <b>{audioType}</b> is null");
            }
            return true;
		}

		public bool IsInSoundBank(int id)
		{
            if (_soundBank.Count < 1)
            {
                LogError($"There isn't any sound asset in {nameof(SoundManager)}!");
                return false;
            }
            else if (!_soundBank.ContainsKey(id))
            {
                LogError($"Audio may not exist in the current SoundAsset,AudioType:{GetAudioType(id)},SoundID:{id}");
                return false;
            }
            return true;
        }
        #endregion

        public void SetVolume(float vol, float fadeTime, AudioType audioType)
        {
            if(audioType == AudioType.All)
			{
                LoopAllAudioType((loopAudioType) => SetPlayerVolume(loopAudioType));
            }
            else
			{
                SetPlayerVolume(audioType);
			}

            void SetPlayerVolume(AudioType target)
			{
                if (TryGetPlayer(target, out var player))
                {
                    player.SetVolume(vol, fadeTime);
                }
            }
        }

		public void StopPlaying(float fadeTime,AudioType audioType)
        {
            if(audioType == AudioType.All)
			{
                LoopAllAudioType((loopAudioType) => Stop(loopAudioType));
            }
            else
			{
                Stop(audioType);
            }

            void Stop(AudioType target)
			{
                if (TryGetPlayer(target, out var player))
                {
                    player.Stop(fadeTime);
                }
            }
        }

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
            else if(id == _currMusicPlayer.CurrentMusicID)
            {
                LogWarning("The music you want to play is already playing");
                return false;
            }
            return true;
        }

		private bool RandomSoundCheck(int id)
        {
            if (_sfxPlayer == null || _randomSoundAsset == null || _randomSoundBank.Count < 1 || !_randomSoundBank.ContainsKey(id))
            {
                LogError($"No sound can play , please check {nameof(SoundManager)}'s setting");
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

