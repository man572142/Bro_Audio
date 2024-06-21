using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Data;
using Ami.Extension;
using System;
using static Ami.BroAudio.Utility;
using static Ami.Extension.CoroutineExtension;
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
                Debug.LogError(Utility.LogTitle + $"Initialize failed ,please check {nameof(SoundManager)}.prefab in your Resources folder!");
                return;
            }

            if (prefab.TryGetComponent(out SoundManager soundSystem))
            {
                _instance = soundSystem;
            }
            else
            {
                Debug.LogError(Utility.LogTitle + $"Initialize failed ,please add {nameof(SoundManager)} component to {nameof(SoundManager)}.prefab");
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
        [SerializeField] BroAudioData _data = null;

        private Dictionary<int, IAudioEntity> _audioBank = new Dictionary<int, IAudioEntity>();

        private Dictionary<BroAudioType, AudioTypePlaybackPreference> _auidoTypePref = new Dictionary<BroAudioType, AudioTypePlaybackPreference>();
        private EffectAutomationHelper _automationHelper = null;

        private Dictionary<SoundID, AudioPlayer> _combFilteringPreventer = null;

        private Coroutine _masterVolumeCoroutine;

        public IReadOnlyDictionary<BroAudioType, AudioTypePlaybackPreference> AudioTypePref => _auidoTypePref;
        public AudioMixer Mixer => _broAudioMixer;

        private void Awake()
		{
            string nullRefLog = LogTitle + $"Please assign {{0}} in {nameof(SoundManager)}.prefab";
            if(!_broAudioMixer)
			{
                Debug.LogError(string.Format(nullRefLog, MixerName));
                return;
			}
            else if(!_audioPlayerPrefab)
			{
                Debug.LogError(string.Format(nullRefLog, AudioPlayerPrefabName));
                return;
			}

            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab, transform, Setting.DefaultAudioPlayerPoolSize, this);
            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups(GenericTrackName);
            AudioMixerGroup[] dominatorGroups = _broAudioMixer.FindMatchingGroups(DominatorTrackName);
            
            _audioTrackPool = new AudioTrackObjectPool(mixerGroups);
            _dominatorTrackPool = new AudioTrackObjectPool(dominatorGroups, true);

            InitBank();
            _automationHelper = new EffectAutomationHelper(this, _broAudioMixer);
        }

		#region InitBank
		private void InitBank()
		{
			foreach (var asset in _data.Assets)
			{
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
                Debug.LogWarning(LogTitle + $"SetVolume with {targetType} is meaningless");
                return;
			}

            GetPlaybackPrefByType(targetType, pref => pref.Volume = vol);
            GetCurrentActivePlayers(player => 
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
            GetCurrentActivePlayers(player =>
            {
                if (player && player.ID == id)
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
            if(effect.Type == EffectType.None)
            {
				mode = SetEffectMode.Override;
			}
            else if(effect.IsDefault())
            {
                mode = SetEffectMode.Remove;
            }

            Action<EffectType> onResetEffect = null;
            if(!effect.IsDominator)
            {
                if(mode == SetEffectMode.Remove)
                {
                    // wait for reset tweaking of the previous effect
                    onResetEffect = (resetType) => SetPlayerEffect(targetType, resetType, SetEffectMode.Remove);
                }
                else
                {
                    SetPlayerEffect(targetType, effect.Type, mode);
                }
			}
            
            _automationHelper.SetEffectTrackParameter(effect, onResetEffect);
            return _automationHelper;
        }

        private void SetPlayerEffect(BroAudioType targetType, EffectType effectType,SetEffectMode mode)
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

			GetCurrentActivePlayers(player =>
			{
                if (targetType.Contains(GetAudioType(player.ID)) && !player.IsDominator)
				{
					player.SetEffect(effectType, mode);
				}
			});
		}
        #endregion

        public void SetPitch(float pitch, BroAudioType targetType, float fadeTime)
        {
            if (targetType == BroAudioType.None)
            {
                Debug.LogWarning(LogTitle + $"SetPitch with {targetType} is meaningless");
                return;
            }

            GetPlaybackPrefByType(targetType, pref => pref.Pitch = pitch);
            GetCurrentActivePlayers(player =>
            {
                if (targetType.Contains(GetAudioType(player.ID)))
                {
                    player.SetPitch(pitch, fadeTime);
                }
            });
        }

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

        private void GetCurrentActivePlayers(Action<AudioPlayer> onGetPlayer)
        {
            // For those which are currently playing.
            var players = _audioPlayerPool.GetCurrentAudioPlayers();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player.IsActive)
                    {
                        onGetPlayer?.Invoke(player);
                    }
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

        public string GetNameByID(int id)
		{
            if(!Application.isPlaying)
			{
               Debug.LogError(LogTitle + $"The method {"GetNameByID".ToWhiteBold()} is {"Runtime Only".ToBold().SetColor(Color.green)}");
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

#if UNITY_EDITOR
        public void SetCoreData(BroAudioData data)
        {
            _data = data;
        } 
#endif
    }
}