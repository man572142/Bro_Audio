using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using System.Linq;
using System;

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

        private const int DefaultPlayerPoolSize = 5;

        [Header("Player")]
        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private AudioPlayerObjectPool _audioPlayerPool = null;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Fading Setting")]
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;

        [Header("Library")]
        [SerializeField] private List<ScriptableObject> _soundAssets = new List<ScriptableObject>();
        private Dictionary<int, IAudioLibrary> _audioBank = new Dictionary<int, IAudioLibrary>();

        private Dictionary<BroAudioType, AudioTypePreference> _auidoTypePref = new Dictionary<BroAudioType, AudioTypePreference>();
        private EffectAutomationHelper _automationHelper = null;

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
                LogError(string.Format(nullRefLog,"AudioPlayer"));
                return;
			}

            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups("Track");
            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab,transform, DefaultPlayerPoolSize, mixerGroups);

			InitBank();
            _automationHelper = new EffectAutomationHelper(this, _broAudioMixer);
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
				for (int i = 0; i < dataList.Count; i++)
				{
					var library = dataList[i];
					if (!library.Validate(i))
                        continue;

                    if (!_audioBank.ContainsKey(library.ID))
                    {
                        _audioBank.Add(library.ID, library);
                    }

                    var audioType = GetAudioType(library.ID);
                    if(!_auidoTypePref.ContainsKey(audioType))
					{
                        _auidoTypePref.Add(audioType, new AudioTypePreference());
					}
                }
			}
		}
		#endregion

        #region Volume
        public void SetVolume(float vol, BroAudioType targetType, float fadeTime)
		{
            ForeachAudioType((audioType) => 
            { 
                if(targetType.HasFlag(audioType))
				{
                    _auidoTypePref[audioType].Volume = vol;
                }       
            });

            SetInUsePlayerVolume(x => targetType.HasFlag(GetAudioType(x.ID)), vol, fadeTime);
        }

		public void SetVolume(float vol, int id, float fadeTime)
		{
            SetInUsePlayerVolume(x => x.ID == id, vol, fadeTime);
        }

        private void SetInUsePlayerVolume(Predicate<AudioPlayer> predicate,float vol,float fadeTime)
        {
            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach(var player in players)
			{
                if (predicate.Invoke(player))
				{
                    player.SetVolume(vol, fadeTime);
                }
			}
        }
		#endregion

		#region Effect
        public IAutoResetWaitable SetEffect(EffectParameter effect)
		{
            return SetEffect(BroAudioType.All,effect);
        }

        public IAutoResetWaitable SetEffect(BroAudioType targetType, EffectParameter effect)
		{
			bool isOn = effect.Type != EffectType.None;
			SetPlayersEffect(targetType, isOn);

			return SetEffectTrackParameter(effect,() => SetPlayersEffect(targetType,false));
		}

        private void SetPlayersEffect(BroAudioType targetType, bool isOn)
        {
            ForeachAudioType((audioType) =>
            {
                if (_auidoTypePref.ContainsKey(audioType) && targetType.HasFlag(audioType))
                {
                    _auidoTypePref[audioType].IsUsingEffect = isOn;
                }
            });

            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach (var player in players)
            {
                var audioType = GetAudioType(player.ID);
                if (targetType.HasFlag(audioType))
                {
                    player.SetEffectMode(isOn);
                }
            }
        }

        public IAutoResetWaitable SetEffectTrackParameter(EffectParameter effect,Action onReset = null)
		{
            _automationHelper.SetEffectTrackParameter(effect, onReset);
            return _automationHelper;
        }
        #endregion

        private bool TryGetNewAudioPlayer(out AudioPlayer player)
        {
            player = _audioPlayerPool.Extract();
            return player != null;
        }

        private IEnumerator PreventCombFiltering(int id,float preventTime)
        {
            _combFilteringPreventer[id] = true;
            yield return new WaitForSeconds(preventTime);
            _combFilteringPreventer[id] = false;
        }

        #region NullChecker
        private bool IsPlayable(int id)
        {
            if (id <= 0)
            {
                LogError("The sound is missing or it has never been assigned. No Sound will play");
                return false;
            }

            if (!_audioBank.ContainsKey(id))
            {
                LogError($"AudioID:{id} may not exist ,please check [BroAudio > Library Manager]");
                return false;
            }

            BroAudioType audioType = GetAudioType(id);
            if (audioType == BroAudioType.All || audioType == BroAudioType.None)
            {
                LogError($"Audio:{id.ToName().ToWhiteBold()} is invalid");
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

            string result = string.Empty;
            if(_audioBank.TryGetValue(id,out var audioLibrary))
			{
                result = audioLibrary.Name;
			}
            return result;
        }
    }
}

