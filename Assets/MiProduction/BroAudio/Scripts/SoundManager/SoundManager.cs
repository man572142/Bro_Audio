using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using System.Linq;

namespace MiProduction.BroAudio.Runtime
{
    [DisallowMultipleComponent]
    public partial class SoundManager : MonoBehaviour
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
        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private ObjectPool<AudioPlayer> _audioPlayerPool = null;

        private AudioPlayer _currMusicPlayer;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Fading Setting")]
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;

        [Header("Library")]
        [SerializeField] private List<ScriptableObject> _soundAssets = new List<ScriptableObject>();
        private Dictionary<int, SoundLibrary> _soundBank = new Dictionary<int, SoundLibrary>();
        private Dictionary<int, MusicLibrary> _musicBank = new Dictionary<int, MusicLibrary>();

        // 防止Haas Effect產生的Comb Filtering效應
        // TODO: 如果是每次播放都在不同聲道就不用
        private Dictionary<int, bool> _combFilteringPreventer = new Dictionary<int, bool>();

        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }

		private void Awake()
		{
            string nullRefLog = $"Please assign {{0}} in {nameof(SoundManager)}.prefab";
            if(!_broAudioMixer)
			{
                LogError(string.Format(nullRefLog,"BroAudioMixer"));
                return;
			}
            else if(!_audioPlayerPrefab)
			{
                LogError(string.Format(nullRefLog,"AudioPlayer.prefab"));
                return;
			}

            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups("Track");
            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab,transform,5, mixerGroups);

            InitAuxTrack();
			InitBank();
		}

		#region InitBank
		private void InitBank()
		{
			foreach (var scriptableObj in _soundAssets)
			{
                IAudioAsset asset = scriptableObj as IAudioAsset;
                if (asset == null)
                    continue;

                List<IAudioLibrary> dataList = asset.GetAllAudioLibraries().ToList();
				for (int s = 0; s < dataList.Count; s++)
				{
					var library = dataList[s];
					if (!library.Validate(s))
                        continue;

                    switch (asset.AudioType)
                    {
                        case BroAudioType.Music:
                        case BroAudioType.Ambience:
                            if (!_musicBank.ContainsKey(library.ID))
                            {
                                _musicBank.Add(library.ID, (MusicLibrary)library);
                            }
                            break;
                        case BroAudioType.UI:
                        case BroAudioType.SFX:
                        case BroAudioType.VoiceOver:
                            if (!_soundBank.ContainsKey(library.ID))
                            {
                                _soundBank.Add(library.ID, (SoundLibrary)library);
                            }
                            break;
                        default:
                            LogError($"Audio:{library.Name} can't add to sound bank because its invalid AudioType:{asset.AudioType}.");
                            break;
                    }
                }
			}
		}
		#endregion

		#region Play
		public IAudioPlayer PlayMusic(int id,Transition transition, float preventTime)
        {
            if (!IsPlayable(id,_musicBank))
                return null;

            if (_currMusicPlayer == null)
            {
                if(TryGetPlayerWithType<AudioPlayer>(out var musicPlayer))
				{
                    _currMusicPlayer = musicPlayer;
                }
                else
				{
                    return null;
				}
            }

            BroAudioClip clip = _musicBank[id].Clip;
            PlaybackPreference pref = new PlaybackPreference(_musicBank[id].Loop);

            switch (transition)
            {
                case Transition.Immediate:
                    _currMusicPlayer.Stop();
                    _currMusicPlayer.Play(id,clip, pref);
                    break;
                case Transition.FadeOutThenFadeIn:
                    _currMusicPlayer.Stop(() => _currMusicPlayer.Play(id,clip,pref));
                    break;
                case Transition.OnlyFadeInNew:
                    _currMusicPlayer.Stop();
                    _currMusicPlayer.Play(id,clip,pref);
                    break;
                case Transition.OnlyFadeOutCurrent:
                    _currMusicPlayer.Stop(() => _currMusicPlayer.Play(id,clip,pref));
                    break;
                case Transition.CrossFade:
                    if (TryGetPlayerWithType<AudioPlayer>(out var otherPlayer))
                    {
                        _currMusicPlayer.Stop();
                        _currMusicPlayer = otherPlayer;
                        otherPlayer.Play(id,clip,pref);
                    }
                    break;
            }
            StartCoroutine(PreventCombFiltering(id,preventTime));
            return _currMusicPlayer;
        }

        public IAudioPlayer Play(int id,float preventTime)
        {
            BroAudioType audioType = GetAudioType(id);
            if (audioType == BroAudioType.Music || audioType == BroAudioType.Ambience)
			{
                return PlayMusic(id, Transition.Immediate,preventTime);
            }

            if (IsPlayable(id,_soundBank) && TryGetPlayerWithType<AudioPlayer>(out var player))
            {
                PlaybackPreference setting = new PlaybackPreference(_soundBank[id].Delay);
                player.Play(id, _soundBank[id].Clip,setting);
                StartCoroutine(PreventCombFiltering(id,preventTime));
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
        #endregion

        #region Volume
        public void SetVolume(float vol, float fadeTime, BroAudioType audioType)
		{
			if (audioType == BroAudioType.All)
			{
				LoopAllAudioType((loopAudioType) => SetPlayerVolume(loopAudioType));
			}
			else
			{
				SetPlayerVolume(audioType);
			}

			void SetPlayerVolume(BroAudioType target)
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
                LogWarning($"Set volume is failed. Audio:{id.ToName().ToWhiteBold()} is not playing.");
			}
        }
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
                if (_audioPlayerPool.TryGetObject(x => GetAudioType(x.ID) == target, out var player))
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
            else
            {
                LogWarning($"Stop playing is failed. Audio:{id.ToName().ToWhiteBold()} is not playing.");
            }
        }
        #endregion

        private bool TryGetPlayerWithType<T>(out T player) where T : AudioPlayer
        {
            var origin = _audioPlayerPool.Extract();
            player = origin as T;

            return player != null;
        }

        private IEnumerator PreventCombFiltering(int id,float preventTime)
        {
            _combFilteringPreventer[id] = true;
            yield return new WaitForSeconds(preventTime);
            _combFilteringPreventer[id] = false;
        }

        #region NullChecker
        private bool IsPlayable<T>(int id, IDictionary<int, T> bank) where T : IAudioLibrary
        {
            if (id <= 0)
            {
                LogError("The sound is missing or it has never been assigned. No Sound will play");
                return false;
            }

            if (!bank.ContainsKey(id))
            {
                LogError($"AudioID:{id} may not exist ,please check [BroAudio > Library Manager]");
                return false;
            }

            BroAudioType audioType = GetAudioType(id);
            if (audioType == BroAudioType.All)
            {
                LogError($"Audio:{id.ToName().ToWhiteBold()} is invalid");
                return false;
            }
            else if (audioType == BroAudioType.Music &&_currMusicPlayer != null && id == _currMusicPlayer.ID)
            {
                LogWarning("The music you want to play is already playing");
                return false;
            }

            if (_combFilteringPreventer.TryGetValue(id, out bool isPreventing) && isPreventing)
            {
                LogWarning($"One of the plays of Audio:{id.ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"Please avoid playing the same sound repeatedly in a very short period of time (e.g., playing it every other frame). " +
                    $"Check [BroAudio > Global Setting] for more information, or change the Haas Effect setting.");
                return false;
            }

            return true;
        }
        #endregion

        public string GetNameByID(int id)
		{
            if(!Application.isPlaying)
			{
                LogError($"The method {"GetNameByID".ToWhiteBold()} is {"Runtime Only".ToBold().SetColor(Color.green)}");
                return null;
			}

            string result = null;
            if(_soundBank.TryGetValue(id,out var soundLibrary))
			{
                result = soundLibrary.Name;
			}
            else if(_musicBank.TryGetValue(id,out var musicLibrary))
			{
                result = musicLibrary.Name;
			}
            return result;
        }

#if UNITY_EDITOR
        public void AddAsset(ScriptableObject asset)
        {
            if(!_soundAssets.Contains(asset))
			{
                _soundAssets.Add(asset);
            }
        }

        public void RemoveDeletedAsset(ScriptableObject asset)
		{
            for(int i = _soundAssets.Count - 1; i >= 0; i--)
			{
                if(_soundAssets[i] == asset)
				{
                    _soundAssets.RemoveAt(i);
				}
			}
		}
#endif
    }
}

// by 咪 2022
// https://github.com/man572142/Bro_Audio.git

