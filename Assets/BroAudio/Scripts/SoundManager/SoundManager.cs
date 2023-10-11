using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;
using static Ami.BroAudio.Utility;
using static Ami.Extension.CoroutineExtension;
using static Ami.BroAudio.Tools.BroLog;
using static Ami.BroAudio.Tools.BroName;
using System.Linq;
using System;

namespace Ami.BroAudio.Runtime
{
    [DisallowMultipleComponent]
    public partial class SoundManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            GameObject prefab = Instantiate(Resources.Load(nameof(SoundManager))) as GameObject;

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

        public const string AudioPlayerPrefabName = "AudioPlayer";

        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private AudioPlayerObjectPool _audioPlayerPool = null;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Library")]
        [SerializeField] private List<ScriptableObject> _soundAssets = new List<ScriptableObject>();
        private Dictionary<int, IAudioEntity> _audioBank = new Dictionary<int, IAudioEntity>();

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
                LogError(string.Format(nullRefLog, MixerName));
                return;
			}
            else if(!_audioPlayerPrefab)
			{
                LogError(string.Format(nullRefLog, AudioPlayerPrefabName));
                return;
			}

            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups(GenericTrackName);
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

				foreach(var library in asset.GetAllAudioLibraries())
				{
					if (!library.Validate())
                        continue;

                    if (!_audioBank.ContainsKey(library.ID))
                    {
                        _audioBank.Add(library.ID, library as IAudioEntity);
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
#if !UNITY_WEBGL
            if(targetType == BroAudioType.All)
			{
                SetMasterVolume(vol,fadeTime);
                return;
			}
#endif
            if (targetType == BroAudioType.None)
			{
                LogWarning($"SetVolume with {targetType} is meaningless");
                return;
			}

            GetPlaybackPrefByType(targetType, pref => pref.Volume = vol);
            GetCurrentInUsePlayers(player => 
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
            if(_broAudioMixer.GetFloat(MasterTrackName,out float currentVol))
			{
				if (currentVol == targetVol)
				{
					return;
				}

                if(fadeTime != 0f)
                {
                    Ease ease = currentVol < targetVol ? FadeInEase : FadeOutEase;
                    var volumes = AnimationExtension.GetLerpValuesPerFrame(currentVol, targetVol, fadeTime, ease);
                    this.StartCoroutineAndReassign(SetMasterVolume(volumes), ref _masterVolumeCoroutine);
                }
				else
                {
                    _broAudioMixer.SetFloat(MasterTrackName, targetVol);
                }
			}

            IEnumerator SetMasterVolume(IEnumerable<float> volumes)
            {
                foreach (var vol in volumes)
                {
                    _broAudioMixer.SetFloat(MasterTrackName, vol);
                    yield return null;
                }
            }
        }

		public void SetVolume(float vol, int id, float fadeTime)
		{
            GetCurrentInUsePlayers(player =>
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

			GetCurrentInUsePlayers(player =>
			{
                if (targetType.HasFlag(GetAudioType(player.ID)) && !player.IsDominator)
				{
					player.SetEffect(effectType, mode);
				}
			});
		}
        #endregion

		private void GetPlaybackPrefByType(BroAudioType targetType, Action<AudioTypePlaybackPreference> onGetPref)
        {
            // For those which may be played in the future.
            ForeachConcreteAudioType((audioType) =>
            {
                if (targetType.HasFlag(audioType) && _auidoTypePref.TryGetValue(audioType,out var pref))
                {
                    onGetPref.Invoke(pref);
                }
            });
        }

        private void GetCurrentInUsePlayers(Action<AudioPlayer> onGetPlayer)
        {
            // For those which are currently playing.
            var players = _audioPlayerPool.GetInUseAudioPlayers();
            foreach (var player in players)
            {
                onGetPlayer?.Invoke(player);
            }
        }

        private bool TryGetAvailablePlayer(int id, out AudioPlayer audioPlayer)
        {
            audioPlayer = null;
            if (AudioPlayer.ResumablePlayers == null || !AudioPlayer.ResumablePlayers.TryGetValue(id, out audioPlayer))
            {
                if (TryGetNewAudioPlayer(out AudioPlayer newPlayer))
                {
                    audioPlayer = newPlayer;
                }
            }

            return audioPlayer != null;
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
        private bool IsPlayable(int id,out IAudioEntity entity)
        {
            entity = null;
            if (id <= 0 || !_audioBank.TryGetValue(id, out entity))
            {
                LogError($"The sound is missing or it has never been assigned. No sound will be played. AudioID:{id}");
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
            if(_audioBank.TryGetValue(id,out var entity))
			{
                IAudioLibrary library = entity as IAudioLibrary;
                result = library?.Name;
			}
            return result;
        }
	}
}

