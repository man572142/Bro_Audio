using MiProduction.Extension;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiProduction.BroAudio.Asset.Core;
using UnityEngine.Audio;
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

        public const int MusicPlayersCount = 2;

        [Header("Player")]
        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private ObjectPool<AudioPlayer> _audioPlayerPool = null;

        private MusicPlayer _currMusicPlayer;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Fading Setting")]
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;

        // 音效
        [Header("Library")]
        [SerializeField] SoundLibraryAsset[] _allSoundAssets = null;
        private Dictionary<int, SoundLibrary> _soundBank = new Dictionary<int, SoundLibrary>();

        // 音樂
        [SerializeField] MusicLibraryAsset _mainMusicAsset = null;
        private Dictionary<int, MusicLibrary> _musicBank = new Dictionary<int, MusicLibrary>();

        // 防止Haas Effect產生的Comb Filtering效應
        // TODO: 如果是每次播放都在不同聲道就不用
        public Dictionary<int, bool> CombFilteringPreventer = new Dictionary<int, bool>();

        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }


        private void Awake()
		{
            string nullRefLog = $"Please asign {{0}} in {nameof(SoundManager)}.prefab";
            if(_broAudioMixer == null)
			{
                LogError(string.Format(nullRefLog,"BroAudioMixer"));
                return;
			}
            else if(_audioPlayerPrefab == null)
			{
                LogError(string.Format(nullRefLog,"AudioPlayer.prefab"));
                return;
			}

            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups("Track");
            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab,5, mixerGroups);

			InitSoundBank();
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
		} 
		#endregion

		#region 音樂

		public IAudioPlayer PlayMusic(int id,Transition transition,float fadeTime = -1)
        {
            if (!IsPlayable(id))
                return null;

            if (_currMusicPlayer == null)
            {
                if(TryGetPlayerWithType<MusicPlayer>(out var musicPlayer))
				{
                    _currMusicPlayer = musicPlayer;
                }
                else
				{
                    return null;
				}
            }

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
                    if (TryGetPlayerWithType<MusicPlayer>(out var otherPlayer))
                    {
                        _currMusicPlayer.Stop(fadeTime, () =>
                        {
                            _audioPlayerPool.Recycle(_currMusicPlayer);
                            _currMusicPlayer = otherPlayer;
                        });
                        otherPlayer.Play(id,clip,isLoop, fadeTime);
                    }
                    break;
            }
            StartCoroutine(PreventCombFiltering(id));
            return _currMusicPlayer;
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
            if(IsPlayable(id) && TryGetPlayerWithType<SoundPlayer>(out var soundPlayer))
            {
                soundPlayer.Play(id, _soundBank[id].Clip, _soundBank[id].Delay, preventTime);
                StartCoroutine(PreventCombFiltering(id));
                return soundPlayer;
            }
            return null;
        }

        public IAudioPlayer PlaySound(int id, Vector3 position)
        {
            if(IsPlayable(id) && TryGetPlayerWithType<SoundPlayer>(out var soundPlayer))
            {
                soundPlayer.PlayAtPoint(id,_soundBank[id].Clip, _soundBank[id].Delay, position);
                StartCoroutine(PreventCombFiltering(id));
                return soundPlayer;
            }
            return null;
        }

        #endregion

        #region PlayerControl
        public void SetVolume(float vol, float fadeTime, AudioType audioType)
		{
			if (audioType == AudioType.All)
			{
				LoopAllAudioType((loopAudioType) => SetPlayerVolume(loopAudioType));
			}
			else
			{
				SetPlayerVolume(audioType);
			}

			void SetPlayerVolume(AudioType target)
            {
                if (_audioPlayerPool.TryGetObject(x => GetAudioType(x.ID) == target, out var player))
				{
                    player.SetVolume(vol,fadeTime);
				}
			}
		}

		public void SetVolume(float vol, float fadeTime,int id)
		{
            if (_audioPlayerPool.TryGetObject(x => x.ID == id, out var player))
			{
                player.SetVolume(vol, fadeTime);
            }
            else
			{
                LogWarning($"Set volume is failed. AudioID:{id} is not playing.");
			}
        }

		public void StopPlaying(float fadeTime, AudioType audioType)
		{
			if (audioType == AudioType.All)
			{
				LoopAllAudioType((loopAudioType) => Stop(loopAudioType));
			}
			else
			{
				Stop(audioType);
			}

			void Stop(AudioType target)
			{
                if (_audioPlayerPool.TryGetObject(x => GetAudioType(x.ID) == target, out var player))
                {
                    player.Stop(fadeTime);
                }
            }
        }

		public void StopPlaying(float fadeTime, int id)
		{
            if (_audioPlayerPool.TryGetObject(x => x.ID == id, out var player))
            {
                player.Stop(fadeTime);
            }
            else
            {
                LogWarning($"Stop playing is failed. AudioID:{id} is not playing.");
            }
        }
        #endregion

        private bool TryGetPlayerWithType<T>(out T player) where T : AudioPlayer
        {
            player = _audioPlayerPool.Extract() as T;
            return player != null;
        }

        private IEnumerator PreventCombFiltering(int id)
        {
            CombFilteringPreventer[id] = true;
            yield return new WaitForSeconds(AudioExtension.HaasEffectInSecond);
            CombFilteringPreventer[id] = false;
        }

        #region NullChecker
        private bool IsPlayable(int id)
        {
            if (id == 0)
            {
                LogError("AudioID is 0 (None). No Sound will play");
                return false;
            }

            if (!_musicBank.ContainsKey(id) && !_soundBank.ContainsKey(id))
            {
                LogError($"AudioID:{id} may not exist ,please check [BroAudio > Library Manager]");
                return false;
            }

            AudioType audioType = GetAudioType(id);
            if (audioType == AudioType.All)
            {
                LogError($"AudioID:{id} is invalid");
                return false;
            }
            else if (audioType == AudioType.Music &&_currMusicPlayer != null && id == _currMusicPlayer.ID)
            {
                LogWarning("The music you want to play is already playing");
                return false;
            }

            if (CombFilteringPreventer.TryGetValue(id, out bool isPreventing) && isPreventing)
            {
                LogWarning($"One of the plays of AudioID:{id} has been rejected due to the concern about sound quality. " +
                    $"Please avoid playing the same sound repeatedly in a very short period of time (e.g., playing it every other frame). " +
                    $"Check [BroAudio > Global Setting] for more information, or change the Haas Effect setting.");
                return false;
            }

            return true;
        }
        #endregion
    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git

