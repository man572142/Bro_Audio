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
    public partial class SoundManager : MonoBehaviour, IAudioMixerPool
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var prefab = Resources.Load(nameof(SoundManager)) as GameObject;
            if (prefab == null)
            {
                Debug.LogError(LogTitle + $"Initialize failed ,please check {nameof(SoundManager)}.prefab in your Resources folder!");
                return;
            }

            prefab.SetActive(false);
            GameObject objectInstance = Instantiate(prefab);
            if (objectInstance.TryGetComponent(out SoundManager manager))
            {
                _instance = manager;
            }
            else
            {
                Debug.LogError(LogTitle + $"Initialize failed ,please add {nameof(SoundManager)} component to {nameof(SoundManager)}.prefab");
                return;
            }

            DontDestroyOnLoad(objectInstance);
            objectInstance.SetActive(true);
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
        private EffectAutomationHelper _dominatorAutomationHelper = null;
        private Dictionary<SoundID, AudioPlayer> _combFilteringPreventer = null;
        private Coroutine _masterVolumeCoroutine;

#if UNITY_WEBGL
        public float WebGLMasterVolume { get; private set; } = AudioConstant.FullVolume;
#endif

        public AudioMixer AudioMixer => _broAudioMixer;
        public IReadOnlyDictionary<SoundID, AudioPlayer> CombFilteringPreventer => _combFilteringPreventer;

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

            _audioPlayerPool = new AudioPlayerObjectPool(_audioPlayerPrefab, transform, Setting.DefaultAudioPlayerPoolSize);
            AudioMixerGroup[] mixerGroups = _broAudioMixer.FindMatchingGroups(GenericTrackName);
            AudioMixerGroup[] dominatorGroups = _broAudioMixer.FindMatchingGroups(DominatorTrackName);
            
            _audioTrackPool = new AudioTrackObjectPool(mixerGroups);
            _dominatorTrackPool = new AudioTrackObjectPool(dominatorGroups, true);

            InitBank();
            _automationHelper = new EffectAutomationHelper(this, _broAudioMixer);
            _dominatorAutomationHelper = new EffectAutomationHelper(this, _broAudioMixer);
        }

        private void OnDestroy()
        {
            MusicPlayer.CleanUp();
            ResetClipSequencer();
        }

        #region InitBank
        private void InitBank()
        {
            foreach (var asset in _data.Assets)
            {
                if (asset == null)
                    continue;

                asset.LinkPlaybackGroup(Setting.GlobalPlaybackGroup);

                foreach(var identity in asset.GetAllAudioEntities())
                {
                    if (!identity.Validate())
                        continue;

                    if (!_audioBank.ContainsKey(identity.ID))
                    {
                        var entity = identity as IAudioEntity;
                        entity.LinkPlaybackGroup(asset.PlaybackGroup);
                        _audioBank.Add(identity.ID, entity);
                    }
                }
            }

            ForeachConcreteAudioType(new PlaybackPrefInitializer() { AudioTypePref = _auidoTypePref });
        }
        #endregion

        #region Volume
        public void SetVolume(float vol, BroAudioType targetType, float fadeTime)
        {
            targetType = targetType.ConvertEverythingFlag();
            if (targetType == BroAudioType.None)
            {
                Debug.LogWarning(LogTitle + $"SetVolume with {targetType} is meaningless");
                return;
            }

            if (targetType == BroAudioType.All)
            {
                SetMasterVolume(vol,fadeTime);
                return;
            }
            SetPlaybackPrefByType(targetType, vol , AudioTypePlaybackPreference.OnSetVolume);
            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && targetType.Contains(GetAudioType(player.ID)))
                {
                    player.SetAudioTypeVolume(vol, fadeTime);
                }
            }
        }

        private void SetMasterVolume(float targetVol, float fadeTime)
        {
#if UNITY_WEBGL
            if (!Mathf.Approximately(WebGLMasterVolume, targetVol))
            {
                if (fadeTime > 0f)
                {
                    this.StartCoroutineAndReassign(SetMasterVolume(WebGLMasterVolume, targetVol, fadeTime), ref _masterVolumeCoroutine);
                }
                else
                {
                    SetWebGLMaster(targetVol);
                }
            }

            IEnumerator SetMasterVolume(float currentVol, float targetVol, float fadeTime)
            {
                Ease ease = currentVol < targetVol ? FadeInEase : FadeOutEase;
                float currentTime = 0f;
                while (currentTime < fadeTime)
                {
                    yield return null;
                    currentTime += Utility.GetDeltaTime();
                    float vol = Mathf.Lerp(currentVol, targetVol, (currentTime / fadeTime).SetEase(ease));
                    SetWebGLMaster(vol);
                }
            }

            void SetWebGLMaster(float targetVol)
            {
                WebGLMasterVolume = targetVol.ClampNormalize();

                foreach (var player in GetCurrentAudioPlayers())
                {
                    if (player.IsActive)
                    {
                        player.UpdateWebGLVolume();
                    }
                }
            }
#else
            targetVol = targetVol.ToDecibel();
            if(_broAudioMixer.SafeGetFloat(MasterTrackName,out float currentVol))
            {
                if (currentVol == targetVol)
                {
                    return;
                }

                if(fadeTime != 0f)
                {
                    this.StartCoroutineAndReassign(SetMasterVolume(currentVol, targetVol, fadeTime), ref _masterVolumeCoroutine);
                }
                else
                {
                    _broAudioMixer.SafeSetFloat(MasterTrackName, targetVol);
                }
            }

            IEnumerator SetMasterVolume(float currentVol, float targetVol, float fadeTime)
            {
                Ease ease = currentVol < targetVol ? FadeInEase : FadeOutEase;
                float currentTime = 0f;
                while (currentTime < fadeTime)
                {
                    yield return null;
                    currentTime += Utility.GetDeltaTime();
                    float vol = Mathf.Lerp(currentVol, targetVol, (currentTime / fadeTime).SetEase(ease));
                    _broAudioMixer.SafeSetFloat(MasterTrackName, vol);
                }
            }
#endif
        }

        public void SetVolume(int id, float vol, float fadeTime)
        {
            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && player.ID == id)
                {
                    player.SetVolume(vol, fadeTime);
                }
            }
        }
        #endregion

        #region Effect
        public IAutoResetWaitable SetEffect(Effect effect)
        {
            return SetEffect(BroAudioType.All,effect);
        }

        public IAutoResetWaitable SetEffect(BroAudioType targetType, Effect effect)
        {
            targetType = targetType.ConvertEverythingFlag();
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
            EffectAutomationHelper automationHelper = null;
            if (effect.IsDominator)
            {
                automationHelper = _dominatorAutomationHelper;
            }
            else
            {
                if (mode == SetEffectMode.Remove)
                {
                    // wait for reset tweaking of the previous effect
                    onResetEffect = (resetType) => SetPlayerEffect(targetType, resetType, SetEffectMode.Remove);
                }
                else
                {
                    SetPlayerEffect(targetType, effect.Type, mode);
                }
                automationHelper = _automationHelper;
            }

            automationHelper.SetEffectTrackParameter(effect, onResetEffect);
            return automationHelper;
        }

        private void SetPlayerEffect(BroAudioType targetType, EffectType effectType,SetEffectMode mode)
        {
            var parameter = new AudioTypePlaybackPreference.SetEffectParameter() { EffectType = effectType, Mode = mode};
            SetPlaybackPrefByType(targetType, parameter, AudioTypePlaybackPreference.OnSetEffect);

            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && targetType.Contains(GetAudioType(player.ID)) && !player.IsDominator)
                {
                    player.SetEffect(effectType, mode);
                }
            }
        }
        #endregion

        public void SetPitch(SoundID id, float pitch, float fadeTime)
        {
            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && player.ID == id)
                {
                    player.SetPitch(pitch, fadeTime);
                }
            }
        }
        
        public void SetPitch(float pitch, BroAudioType targetType, float fadeTime)
        {
            targetType = targetType.ConvertEverythingFlag();
            if (targetType == BroAudioType.None)
            {
                Debug.LogWarning(LogTitle + $"SetPitch with {targetType} is meaningless");
                return;
            }

            SetPlaybackPrefByType(targetType, pitch, AudioTypePlaybackPreference.OnSetpitch);
            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && targetType.Contains(GetAudioType(player.ID)))
                {
                    player.SetPitch(pitch, fadeTime);
                }
            }
        }

        public bool TryGetAudioTypePref(BroAudioType audioType, out IAudioPlaybackPref result)
        {
            result = null;
            if (_auidoTypePref.TryGetValue(audioType, out var typePref))
            {
                result = typePref;
            }
            return result != null;
        }

        public bool HasAnyPlayingInstances(SoundID id)
        {
            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && player.ID == id && player.IsPlaying)
                {
                    return true;
                }
            }
            return false;
        }

        AudioMixerGroup IAudioMixerPool.GetTrack(AudioTrackType trackType) => trackType switch
        {
            AudioTrackType.Generic => _audioTrackPool.Extract(),
            AudioTrackType.Dominator => _dominatorTrackPool.Extract(),
            _ => null,
        };

        void IAudioMixerPool.ReturnTrack(AudioTrackType trackType, AudioMixerGroup track)
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

        // For those which may be played in the future.
        private void SetPlaybackPrefByType<TParameter>(BroAudioType targetType, TParameter parameter, Action<AudioTypePlaybackPreference, TParameter> onModifyPref) where TParameter : struct
        {
            var setter = new PlaybackPrefSetter<TParameter>()
            {
                TargetType = targetType,
                AudioTypePref = _auidoTypePref,
                OnModifyPref = onModifyPref,
                Parameter = parameter,
            };

            ForeachConcreteAudioType(setter);
        }

        #region Audio Player
        void IAudioMixerPool.ReturnPlayer(AudioPlayer player)
        {
            RemoveFromPreventer(player);
            _audioPlayerPool.Recycle(player);
        }

        private IReadOnlyList<AudioPlayer> GetCurrentAudioPlayers()
        {
            return _audioPlayerPool.GetCurrentAudioPlayers();
        }
        #endregion
    }
}