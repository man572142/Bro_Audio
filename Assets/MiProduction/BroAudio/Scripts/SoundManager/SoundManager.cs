using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.BroAudio.Data;
using MiProduction.Extension;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.CoroutineExtension;
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

        private const string MasterTrackExposedParameter = "Master";
        private const string GenericTrackExposedParameter = "Track";

        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private AudioPlayerObjectPool _audioPlayerPool = null;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Library")]
        [SerializeField] private List<ScriptableObject> _soundAssets = new List<ScriptableObject>();
        private Dictionary<int, IAudioLibrary> _audioBank = new Dictionary<int, IAudioLibrary>();

        private Dictionary<BroAudioType, AudioTypePlaybackPreference> _auidoTypePref = new Dictionary<BroAudioType, AudioTypePlaybackPreference>();
        private EffectAutomationHelper _automationHelper = null;

        // 防止Haas Effect產生的Comb Filtering效應
        // TODO: 如果是每次播放都在不同聲道就不用
        private Dictionary<int, bool> _combFilteringPreventer = new Dictionary<int, bool>();

        private Coroutine _masterVolumeCoroutine;

        public IReadOnlyDictionary<BroAudioType, AudioTypePlaybackPreference> AudioTypePref => _auidoTypePref;
        public AudioMixer Mixer => _broAudioMixer;

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

            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups(GenericTrackExposedParameter);
            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab,transform,Setting.DefaultAudioPlayerPoolSize, mixerGroups);

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
					if (!library.Validate())
                        continue;

                    if (!_audioBank.ContainsKey(library.ID))
                    {
                        _audioBank.Add(library.ID, library);
                    }

                    var audioType = GetAudioType(library.ID);
                    if(!_auidoTypePref.ContainsKey(audioType))
					{
                        _auidoTypePref.Add(audioType, new AudioTypePlaybackPreference());
					}
                }
			}
		}
		#endregion

        #region Volume
        public void SetVolume(float vol, BroAudioType targetType, float fadeTime)
		{
            if(targetType == BroAudioType.All)
			{
                SetMasterVolume(vol,fadeTime);
                return;
			}
            else if (targetType == BroAudioType.None)
			{
                LogWarning($"SetVolume with {targetType} is meaningless");
                return;
			}

            GetPlaybackPrefByType(targetType, pref => pref.Volume = vol);
            GetCurrentPlayers(player => 
            { 
                if(targetType.HasFlag(GetAudioType(player.ID)))
				{
                    player.SetVolume(vol, fadeTime);
                }
            });
        }

		private void SetMasterVolume(float targetVol, float fadeTime)
		{
            targetVol = targetVol.ToDecibel();
            if(_broAudioMixer.GetFloat(MasterTrackExposedParameter,out float currentVol))
			{
				if (currentVol == targetVol)
				{
					return;
				}

				Ease ease = currentVol < targetVol ? FadeInEase : FadeOutEase;
				var volumes = AnimationExtension.GetLerpValuesPerFrame(currentVol, targetVol, fadeTime, ease);

				this.StartCoroutineAndReassign(SetMasterVolume(volumes),ref _masterVolumeCoroutine);
			}

            IEnumerator SetMasterVolume(IEnumerable<float> volumes)
            {
                foreach (var vol in volumes)
                {
                    _broAudioMixer.SetFloat(MasterTrackExposedParameter, vol);
                    yield return null;
                }
            }
        }

		public void SetVolume(float vol, int id, float fadeTime)
		{
            GetCurrentPlayers(player =>
            {
                if (player.ID == id)
                {
                    player.SetVolume(vol, fadeTime);
                }
            });
        }
		#endregion

		#region Effect
        public IAutoResetWaitable SetEffect(EffectParameter effect)
		{
            return SetEffect(BroAudioType.All,effect);
        }

        public IAutoResetWaitable SetEffect(BroAudioType targetType, EffectParameter effect)
		{
			SetEffectMode mode = effect.Type == default ? SetEffectMode.Override : SetEffectMode.Add;
			SetPlayersEffect(targetType, effect.Type, mode);
            
            _automationHelper.SetEffectTrackParameter(effect, (resetType) => SetPlayersEffect(targetType, resetType,SetEffectMode.Remove));
            return _automationHelper;
        }

        private void SetPlayersEffect(BroAudioType targetType, EffectType effectType,SetEffectMode mode)
        {
			GetPlaybackPrefByType(targetType, pref =>
			{
                switch (mode)
				{
					case SetEffectMode.Add:
                        pref.EffectType |= effectType;
						break;
					case SetEffectMode.Remove:
						pref.EffectType &= ~effectType;
						break;
					case SetEffectMode.Override:
						pref.EffectType = effectType;
						break;
				}				
			});

			GetCurrentPlayers(player =>
			{
                if (targetType.HasFlag(GetAudioType(player.ID)))
				{
					player.SetEffect(effectType, mode);
				}
			});
		}
		#endregion

		private void GetPlaybackPrefByType(BroAudioType targetType, Action<AudioTypePlaybackPreference> onGetPref)
        {
            // For those which may be played in the future.
            ForeachAudioType((audioType) =>
            {
                if (targetType.HasFlag(audioType) && _auidoTypePref.TryGetValue(audioType,out var pref))
                {
                    onGetPref.Invoke(pref);
                }
            });
        }

        private void GetCurrentPlayers(Action<AudioPlayer> onGetPlayer)
        {
            // For those which are currently playing.
            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach (var player in players)
            {
                onGetPlayer?.Invoke(player);
            }
        }

        private bool TryGetNewAudioPlayer(out AudioPlayer player)
        {
            player = _audioPlayerPool.Extract();
            return player != null;
        }

        private AudioPlayer GetNewAudioPlayer()
		{
            return _audioPlayerPool.Extract();
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
                    $"Check [BroAudio > Global Setting] for more information, or change the setting.");
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

