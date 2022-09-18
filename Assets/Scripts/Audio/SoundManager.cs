using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
using MiProduction.Extension;
using UnityEditor;

namespace MiProduction.BroAudio
{
    public class SoundManager : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            GameObject prefab = Instantiate(Resources.Load("SoundManager")) as GameObject;

            if (prefab == null)
            {
                Debug.LogError("[SoundSystem] initialize failed ,please check SoundManager.prefab in your Resources folder!");
                return;
            }

            if (prefab.TryGetComponent(out SoundManager soundManager))
            {
                Instance = soundManager;
            }
            else
            {
                Debug.LogError("[SoundSystem] initialize failed ,please add SoundManager component to SoundManager.prefab");
            }

            DontDestroyOnLoad(prefab);
        }

        static public SoundManager Instance = null;

        [SerializeField] AudioSource _sfxPlayer;
        [SerializeField] MusicPlayer[] _musicPlayers = null;
        [SerializeField] Ease _fadeInEase = Ease.InCubic;
        [SerializeField] Ease _fadeOutEase = Ease.OutSine;
        MusicPlayer currentPlayer;

        //音效
        Dictionary<Sound, SoundLibrary> _soundBank = new Dictionary<Sound, SoundLibrary>();
        [SerializeField] SoundLibrary[] _soundLibrary = null;
        public SoundLibrary[] SoundLibraries { get => _soundLibrary; }

        //隨機播放音效
        Dictionary<Sound, RandomSoundLibrary> _randomSoundBank = new Dictionary<Sound, RandomSoundLibrary>();
        [SerializeField] RandomSoundLibrary[] _randomSoundLibrary = null;
        public RandomSoundLibrary[] RandomSoundLibraries { get => _randomSoundLibrary; }

        //音樂
        Dictionary<Music, MusicLibrary> _musicBank = new Dictionary<Music, MusicLibrary>();
        [SerializeField] MusicLibrary[] _musicLibrary = null;
        public MusicLibrary[] MusicLibraries { get => _musicLibrary; }

        Dictionary<Sound, bool> _preventPlayback = new Dictionary<Sound, bool>();

        // 還有點問題

        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }


        private void Awake()
        {
            if (_musicPlayers.Length < 2)
            {
                Debug.LogError("[SoundSystem] Please add at least 2 MusicPlayer to SoundManager");
            }
            //初始化音效庫
            for (int s = 0; s < _soundLibrary.Length;s++)
            {
                if (_soundLibrary[s].Validate(s))
                {
                    _soundBank.Add(_soundLibrary[s].Sound, _soundLibrary[s]);
                    _preventPlayback.Add(_soundLibrary[s].Sound, false);
                }      
            }
            // 初始化隨機播放音效庫
            for (int r = 0; r < _randomSoundLibrary.Length; r++)
            {
                if (_randomSoundLibrary[r].Validate(r))
                {
                    _randomSoundBank.Add(_randomSoundLibrary[r].Sound, _randomSoundLibrary[r]);
                    _preventPlayback.Add(_randomSoundLibrary[r].Sound, false);
                }
            }
            //初始化音樂庫
            for (int m = 0; m < _musicLibrary.Length; m++)
            {
                if (_musicLibrary[m].Validate(m))
                {
                    _musicBank.Add(_musicLibrary[m].Music, _musicLibrary[m]);
                }
            }
        }

        private void Start()
        {        
            currentPlayer = _musicPlayers[0];
        }


        #region 音樂
        /// <summary>
        /// 播放音樂(立即播放與停止)
        /// </summary>
        /// <param name="newMusic"></param>
        public void PlayMusic(Music newMusic)
        {
            PlayMusic(newMusic, Transition.Immediate);
        }

        /// <summary>
        /// 播放音樂
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">音樂過渡類型</param>
        /// <param name="fadeTime">若為-1則會採用Library當中所設定的值</param>
        public void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f)
        {
            if (_musicBank.Count < 1 /*|| newMusic == Music.None*/)
            {
                Debug.LogError("[SoundSystem] No music can play!");
                return;
            }

            switch (transition)
            {
                case Transition.Immediate:
                    currentPlayer.Stop(0f);
                    currentPlayer.Play(_musicBank[newMusic], 0f);
                    break;
                case Transition.FadeOutThenFadeIn:
                    currentPlayer.Stop(fadeTime, () => currentPlayer.Play(_musicBank[newMusic], fadeTime));
                    break;
                case Transition.OnlyFadeInNew:
                    currentPlayer.Stop(0f);
                    currentPlayer.Play(_musicBank[newMusic], fadeTime);
                    break;
                case Transition.OnlyFadeOutCurrent:
                    currentPlayer.Stop(fadeTime, () => currentPlayer.Play(_musicBank[newMusic], 0f));
                    break;
                case Transition.CrossFade:
                    if (GetAvailableMusicPlayer(out MusicPlayer otherPlayer))
                    {
                        currentPlayer.Stop(fadeTime, () => currentPlayer = otherPlayer);
                        otherPlayer.Play(_musicBank[newMusic], fadeTime);
                    }
                    else
                    {
                        Debug.LogError("[SoundSystem] No playable music player for another music!");
                    }
                    break;
            }
        }

        /// <summary>
        /// 取得目前可用的Music Player
        /// </summary>
        /// <param name="musicPlayer"></param>
        /// <returns></returns>
        public bool GetAvailableMusicPlayer(out MusicPlayer musicPlayer)
        {
            musicPlayer = null;
            foreach (MusicPlayer player in _musicPlayers)
            {
                if (!player.IsPlaying && player != currentPlayer)
                {
                    musicPlayer = player;
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 音效
        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="sound"></param>
        public void PlaySFX(Sound sound)
        {
            //StartCoroutine(PlayOnce(sound, 0.1f));
            StartCoroutine(PlayOnce(sound, _soundBank[sound].Clip, _soundBank[sound].Delay, _soundBank[sound].Volume, 0.1f));
        }

        /// <summary>
        /// 播放
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime">限制該時間內不能再播放</param>
        public void PlaySFX(Sound sound, float preventTime)
        {
            //StartCoroutine(PlayOnce(sound, preventTime));
            StartCoroutine(PlayOnce(sound,_soundBank[sound].Clip, _soundBank[sound].Delay, _soundBank[sound].Volume, preventTime));
        }

        /// <summary>
        /// 於場景中的指定地點播放
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position"></param>
        public void PlaySFX(Sound sound, Vector3 position)
        {
            StartCoroutine(PlayInScene(sound, position));
        }

        public void PlayRandomSFX(Sound sound)
        {
            StartCoroutine(PlayOnce(sound,GetRandomClip(sound), _soundBank[sound].Delay, _soundBank[sound].Volume, 0.1f));
        }

        public void PlayRandomSFX(Sound sound, float preventTime)
        {
            StartCoroutine(PlayOnce(sound,GetRandomClip(sound), _soundBank[sound].Delay, _soundBank[sound].Volume, preventTime));
        }

        private IEnumerator PlayOnce(Sound sound,AudioClip clip,float delay,float volume,float preventTime)
        {
            yield return new WaitForSeconds(delay);
            if (_preventPlayback[sound])
                yield break;

            _sfxPlayer.PlayOneShot(clip, volume);
            if (preventTime > 0)
                StartCoroutine(PreventPlaybackControl(sound, preventTime));
        }

        private IEnumerator PlayInScene(Sound sound, Vector3 pos)
        {
            yield return new WaitForSeconds(_soundBank[sound].Delay);
            AudioSource.PlayClipAtPoint(_soundBank[sound].Clip, pos, _soundBank[sound].Volume);
            yield break;
        }

        private AudioClip GetRandomClip(Sound sound)
        {
            return _randomSoundBank[sound].Clips[UnityEngine.Random.Range(0, _randomSoundBank[sound].Clips.Length)];
        }

        #endregion

        IEnumerator PreventPlaybackControl(Sound sound,float time)
        {
            _preventPlayback[sound] = true;
            yield return new WaitForSeconds(time);
            _preventPlayback[sound] = false;
        }
    }

    [System.Serializable]
    public struct SoundLibrary : IAudioLibrary
    {
        public string Name;
        public AudioClip Clip;
        public Sound Sound;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float Delay;
        [Min(0f)] public float StartPosition;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(SoundLibrary),index, Clip, StartPosition);
        }
    }

    [System.Serializable]
    public struct RandomSoundLibrary :IAudioLibrary
    {
        public string Name;
        public AudioClip[] Clips;
        public Sound Sound;
        [Range(0f, 1f)] public float Volume;
        [Min(0f)] public float StartPosition;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(RandomSoundLibrary),index,Clips,StartPosition);
        }
    }

    [System.Serializable]
    public struct MusicLibrary : IAudioLibrary
    {
        public string Name;
        public AudioClip Clip;
        public Music Music;
        [Range(0f, 1f)] public float Volume;
        public float StartPosition;
        //[MinMaxSlider(0f,1f)] public Vector2 fade;
        [Min(0f)] public float FadeIn;
        [Min(0f)] public float FadeOut;
        [Min(0f)] public bool Loop;

        public bool Validate(int index)
        {
            return AudioExtension.Validate(nameof(MusicLibrary),index, Clip, StartPosition, FadeIn, FadeOut);
        }
    }
}

