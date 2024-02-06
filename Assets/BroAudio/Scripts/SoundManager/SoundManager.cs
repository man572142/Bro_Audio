using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;
using System;
using static Ami.BroAudio.Utility;
using static Ami.Extension.CoroutineExtension;
using static Ami.BroAudio.Tools.BroLog;
using static Ami.BroAudio.Tools.BroName;

namespace Ami.BroAudio.Runtime
{
    [DisallowMultipleComponent, AddComponentMenu("")]
    public partial class SoundManager : MonoBehaviour, IAudioMixer
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
                _instance = soundSystem;
            }
            else
            {
                LogError($"Initialize failed ,please add {nameof(SoundManager)} component to {nameof(SoundManager)}.prefab");
            }

            DontDestroyOnLoad(prefab);
        }

        private static SoundManager _instance;
        public static SoundManager Instance 
        { 
            get 
            {
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					return null;
				} 
#endif
				return _instance;
            } 
        }

        [SerializeField] AudioPlayer _audioPlayerPrefab = null;
        private AudioPlayerObjectPool _audioPlayerPool = null;
        private ObjectPool<AudioMixerGroup> _audioTrackPool = null;
        private ObjectPool<AudioMixerGroup> _dominatorTrackPool = null;

        [SerializeField] AudioMixer _broAudioMixer = null;

        [Header("Assets")]
        [SerializeField] private List<ScriptableObject> _soundAssets = new List<ScriptableObject>();
        private Dictionary<int, IAudioEntity> _audioBank = new Dictionary<int, IAudioEntity>();

        private Dictionary<BroAudioType, AudioTypePlaybackPreference> _auidoTypePref = new Dictionary<BroAudioType, AudioTypePlaybackPreference>();
        private EffectAutomationHelper _automationHelper = null;

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

            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab, transform, Setting.DefaultAudioPlayerPoolSize, this);
            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups(GenericTrackName);
            AudioMixerGroup[] dominatorGroups = _broAudioMixer.FindMatchingGroups(DominatorTrackName);
            
            _audioTrackPool = new AudioTrackObjectPool(mixerGroups);
            _dominatorTrackPool = new AudioTrackObjectPool(dominatorGroups);

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

				foreach(var entity in asset.GetAllAudioEntities())
				{
					if (!entity.Validate())
                        continue;

                    if (!_audioBank.ContainsKey(entity.ID))
                    {
                        _audioBank.Add(entity.ID, entity as IAudioEntity);
                    }
                }
			}

            ForeachConcreteAudioType(audioType => _auidoTypePref.Add(audioType, new AudioTypePlaybackPreference()));
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
            GetCurrentPlayingPlayers(player => 
            { 
                if(targetType.Contains(GetAudioType(player.ID)))
				{
                    player.SetVolume(vol, fadeTime);
                }
            });
        }

		private void SetMasterVolume(float targetVol, float fadeTime)
		{
            targetVol = targetVol.ToDecibel();
            if(_broAudioMixer.SafeGetFloat(MasterTrackName,out float currentVol))
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
                    _broAudioMixer.SafeSetFloat(MasterTrackName, targetVol);
                }
			}

            IEnumerator SetMasterVolume(IEnumerable<float> volumes)
            {
                foreach (var vol in volumes)
                {
                    _broAudioMixer.SafeSetFloat(MasterTrackName, vol);
                    yield return null;
                }
            }
        }

		public void SetVolume(int id, float vol, float fadeTime)
		{
            GetCurrentPlayingPlayers(player =>
            {
                if (player.ID == id)
                {
                    player.SetVolume(vol, fadeTime);
                }
            });
        }
#endregion

        #region Effect
        public IAutoResetWaitable SetEffect(Effect effect)
		{
            return SetEffect(BroAudioType.All,effect);
        }

        public IAutoResetWaitable SetEffect(BroAudioType targetType, Effect effect)
		{
			SetEffectMode mode = SetEffectMode.Add;
            if(effect.IsDefault())
            {
                mode = SetEffectMode.Remove;
            }
            else if(effect.Type == EffectType.None)
            {
                mode = SetEffectMode.Override;
            }
            SetEffectInternal(targetType, effect.Type, mode);
            
            _automationHelper.SetEffectTrackParameter(effect, (resetType) => SetEffectInternal(targetType, resetType,SetEffectMode.Remove));
            return _automationHelper;
        }

        private void SetEffectInternal(BroAudioType targetType, EffectType effectType,SetEffectMode mode)
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

			GetCurrentPlayingPlayers(player =>
			{
                if (targetType.Contains(GetAudioType(player.ID)) && !player.IsDominator)
				{
					player.SetEffect(effectType, mode);
				}
			});
		}
        #endregion

        AudioMixerGroup IAudioMixer.GetTrack(AudioTrackType trackType)
        {
            switch (trackType)
            {
                case AudioTrackType.Generic:
                    return _audioTrackPool.Extract();
                case AudioTrackType.Dominator:
                    return _dominatorTrackPool.Extract();
            }
            return null;
        }

        void IAudioMixer.ReturnTrack(AudioTrackType trackType, AudioMixerGroup track)
        {
            switch (trackType)
            {
                case AudioTrackType.Generic:
                    _audioTrackPool.Recycle(track);
                    break;
                case AudioTrackType.Dominator:
                    _dominatorTrackPool.Recycle(track);
                    break;
            }
        }

        private void GetPlaybackPrefByType(BroAudioType targetType, Action<AudioTypePlaybackPreference> onGetPref)
        {
            // For those which may be played in the future.
            ForeachConcreteAudioType((audioType) =>
            {
                if (targetType.Contains(audioType) && _auidoTypePref.TryGetValue(audioType,out var pref))
                {
                    onGetPref.Invoke(pref);
                }
            });
        }

        private void GetCurrentPlayingPlayers(Action<AudioPlayer> onGetPlayer)
        {
            // For those which are currently playing.
            var players = _audioPlayerPool.GetCurrentAudioPlayers();
            foreach (var player in players)
            {
                if(player.IsPlaying)
                {
					onGetPlayer?.Invoke(player);
				}
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

            if (_combFilteringPreventer.TryGetValue(id, out bool isPreventing) && isPreventing)
            {
                if(Setting.LogCombFilteringWarning)
				{
                    LogWarning($"One of the plays of Audio:{id.ToName().ToWhiteBold()} has been rejected due to the concern about sound quality. " +
                    $"Check documentation for further details, or change the setting in [BroAudio > Setting].");
                }
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
                IEntityIdentity entityIdentity = entity as IEntityIdentity;
                result = entityIdentity?.Name;
			}
            return result;
        }
    }
}

