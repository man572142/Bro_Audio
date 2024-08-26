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
using static Ami.Extension.AnimationExtension;

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
                Debug.LogError(LogTitle + $"Initialize failed ,please check {nameof(SoundManager)}.prefab in your Resources folder!");
                return;
            }

            if (prefab.TryGetComponent(out SoundManager soundSystem))
            {
                _instance = soundSystem;
            }
            else
            {
                Debug.LogError(LogTitle + $"Initialize failed ,please add {nameof(SoundManager)} component to {nameof(SoundManager)}.prefab");
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
        private EffectAutomationHelper _dominatorAutomationHelper = null;
        private Dictionary<SoundID, AudioPlayer> _combFilteringPreventer = null;
        private Coroutine _masterVolumeCoroutine;

#if UNITY_WEBGL
        public float WebGLMasterVolume { get; private set; } = AudioConstant.FullVolume;
#endif

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
            _dominatorAutomationHelper = new EffectAutomationHelper(this, _broAudioMixer);
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

            SetPlaybackPrefByType(targetType, vol , AudioTypePlaybackPreference.SetVolume);
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
            if (WebGLMasterVolume != targetVol)
            {
                if (fadeTime != 0f)
                {
                    this.StartCoroutineAndReassign(SetMasterVolume(WebGLMasterVolume, targetVol, fadeTime, OnSetMaster), ref _masterVolumeCoroutine);
                }
            }
            
            void OnSetMaster(float vol)
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
                    this.StartCoroutineAndReassign(SetMasterVolume(currentVol, targetVol, fadeTime, OnSetMasterVolume), ref _masterVolumeCoroutine);
                }
                else
                {
                    _broAudioMixer.SafeSetFloat(MasterTrackName, targetVol);
                }
            }

            void OnSetMasterVolume(float vol)
            {
                _broAudioMixer.SafeSetFloat(MasterTrackName, vol);
            }
#endif
        }

        private IEnumerator SetMasterVolume(float currentVol, float targetVol, float fadeTime, Action<float> onSetMaster)
        {
            Ease ease = currentVol < targetVol ? FadeInEase : FadeOutEase;
            yield return LerpValuesPerFrame(currentVol, targetVol, fadeTime, ease, onSetMaster);
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
            SetPlaybackPrefByType(targetType, parameter, AudioTypePlaybackPreference.SetEffect);

            foreach (var player in GetCurrentAudioPlayers())
            {
                if (player.IsActive && targetType.Contains(GetAudioType(player.ID)) && !player.IsDominator)
                {
                    player.SetEffect(effectType, mode);
                }
            }
        }
        #endregion

        public void SetPitch(float pitch, BroAudioType targetType, float fadeTime)
        {
            if (targetType == BroAudioType.None)
            {
                Debug.LogWarning(LogTitle + $"SetPitch with {targetType} is meaningless");
                return;
            }

            SetPlaybackPrefByType(targetType, pitch, AudioTypePlaybackPreference.SetPitch);
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

        AudioMixerGroup IAudioMixer.GetTrack(AudioTrackType trackType) => trackType switch
        {
            AudioTrackType.Generic => _audioTrackPool.Extract(),
            AudioTrackType.Dominator => _dominatorTrackPool.Extract(),
            _ => null,
        };

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

        private void SetPlaybackPrefByType<TParameter>(BroAudioType targetType, TParameter parameter, Action<AudioTypePlaybackPreference, TParameter> onModifyPref) where TParameter : struct
        {
            // For those which may be played in the future.
            ForeachConcreteAudioType((audioType) =>
            {
                if (targetType.Contains(audioType) && _auidoTypePref.TryGetValue(audioType, out var pref))
                {
                    onModifyPref.Invoke(pref, parameter);
                }
            });
        }

        #region Audio Player
        private IReadOnlyList<AudioPlayer> GetCurrentAudioPlayers()
        {
            return _audioPlayerPool.GetCurrentAudioPlayers();
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
        #endregion
    }
}