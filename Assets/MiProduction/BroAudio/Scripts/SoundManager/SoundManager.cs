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

        private Dictionary<BroAudioType, bool> _effectStateDict = new Dictionary<BroAudioType, bool>();
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
                    if(!_effectStateDict.ContainsKey(audioType))
					{
                        _effectStateDict.Add(audioType, false);
					}
                }
			}
		}
		#endregion

        #region Volume
        public void SetVolume(float vol, BroAudioType audioType, float fadeTime)
		{
			if (audioType == BroAudioType.All)
			{
                SetSystemVolume(vol,fadeTime);
			}
			else
			{
				SetPlayerVolume(x => audioType.HasFlag(GetAudioType(x.ID)) , vol, fadeTime);
			}
		}

		public void SetVolume(float vol, int id, float fadeTime)
		{
            SetPlayerVolume(x => x.ID == id, vol, fadeTime);
        }

        private void SetPlayerVolume(Predicate<AudioPlayer> predicate,float vol,float fadeTime)
        {
            if (_audioPlayerPool.TryGetObject(predicate, out var player))
            {
                player.SetVolume(vol, fadeTime);
            }
        }

        public void SetSystemVolume(float vol , float fadeTime)
		{
            var effect = new EffectParameter()
            {
                Value = vol.ToDecibel(),
                FadeTime = fadeTime,
                Type = EffectType.Volume
            };

            //SetEffectTrackParameter(effect, null);
            // todo: 待處理
        }
		#endregion

		#region Effect
        public IAutoResetWaitable SetEffect(EffectParameter effect)
		{
            bool isOn = effect.Type != EffectType.None;
            _effectStateDict[BroAudioType.All] = isOn;

            if (_audioPlayerPool.TryGetInUseAudioPlayers(out var players))
            {
                foreach(var player in players)
				{
					SetPlayerEffect(isOn, player);
				}
			}
            
            return SetEffectTrackParameter(effect);        
        }

		public IAutoResetWaitable SetEffect(BroAudioType audioType, EffectParameter effect)
        {
            LoopAllAudioType((key) => 
            { 
                if(_effectStateDict.ContainsKey(key))
                {
                    _effectStateDict[key] = audioType.HasFlag(key);
                }
            });

            if(_audioPlayerPool.TryGetInUseAudioPlayers(out var players))
			{
                foreach (var player in players)
                {
                    bool isOn = audioType.HasFlag(GetAudioType(player.ID));
                    SetPlayerEffect(isOn, player);
                }
            }

            return SetEffectTrackParameter(effect);
        }

        private void SetPlayerEffect(bool isOn, AudioPlayer player)
        {
            player.SetEffectMode(isOn);
            if (isOn)
            {
                _automationHelper.OnResetEffect += DisableEffect;
            }

            void DisableEffect()
            {
                _automationHelper.OnResetEffect -= DisableEffect;
                if (player)
                {
                    player.SetEffectMode(false);
                }
            }
        }

        public IAutoResetWaitable SetEffectTrackParameter(EffectParameter effect)
		{
            _automationHelper.SetEffectTrackParameter(effect);
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

