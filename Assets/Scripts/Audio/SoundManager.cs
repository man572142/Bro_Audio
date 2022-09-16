using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System.Linq;
using MiProduction.Extension;


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

        //����
        Dictionary<Sound, SoundLibrary> _soundBank = new Dictionary<Sound, SoundLibrary>();
        [SerializeField] SoundLibrary[] _soundLibrary = null;

        //�H�����񭵮�
        Dictionary<Sound, RandomSoundLibrary> _randomSoundBank = new Dictionary<Sound, RandomSoundLibrary>();
        [SerializeField] RandomSoundLibrary[] _randomSoundLibrary = null;

        //����
        Dictionary<Music, MusicLibrary> _musicBank = new Dictionary<Music, MusicLibrary>();
        [SerializeField] MusicLibrary[] _musicLibrary = null;

        bool _isPreventPlayback = false;
        Coroutine _prevenPlayback;

        public static Ease FadeInEase { get => Instance._fadeInEase; }
        public static Ease FadeOutEase { get => Instance._fadeOutEase; }


        private void Awake()
        {
            //��l�ƭ��Įw
            foreach (SoundLibrary s in _soundLibrary)
            {
                _soundBank.Add(s.sound, s);
            }
            // ��l���H�����񭵮Įw
            foreach (RandomSoundLibrary rs in _randomSoundLibrary)
            {
                _randomSoundBank.Add(rs.sound, rs);
            }
            //��l�ƭ��֮w
            foreach (MusicLibrary m in _musicLibrary)
            {
                _musicBank.Add(m.music, m);
            }
        }

        private void Start()
        {
            if (_musicPlayers.Length < 2)
            {
                Debug.LogError("[SoundSystem] Please add at least 2 MusicPlayer to SoundManager");
            }
            currentPlayer = _musicPlayers[0];
        }


        #region ����
        /// <summary>
        /// ���񭵼�(�ߧY����P����)
        /// </summary>
        /// <param name="newMusic"></param>
        public void PlayMusic(Music newMusic)
        {
            PlayMusic(newMusic, Transition.Immediate);
        }

        /// <summary>
        /// ���񭵼�
        /// </summary>
        /// <param name="newMusic"></param>
        /// <param name="transition">���ֹL������</param>
        /// <param name="fadeTime">�Y��-1�h�|�ĥ�Library���ҳ]�w����</param>
        public void PlayMusic(Music newMusic, Transition transition, float fadeTime = -1f)
        {
            if (_musicBank.Count < 1 || newMusic == Music.None)
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
        /// ���o�ثe�i�Ϊ�Music Player
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

        #region ����
        /// <summary>
        /// ����
        /// </summary>
        /// <param name="sound"></param>
        public void PlaySFX(Sound sound)
        {
            StartCoroutine(PlayOnce(sound, 0.1f));
        }

        /// <summary>
        /// ����
        /// <para>preventTime:����Ӯɶ�������A����</para>
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="preventTime"></param>
        public void PlaySFX(Sound sound, float preventTime)
        {
            StartCoroutine(PlayOnce(sound, preventTime));
        }

        /// <summary>
        /// ������������w�a�I����
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position"></param>
        public void PlaySFX(Sound sound, Vector3 position = default(Vector3))
        {
            StartCoroutine(PlayInScene(sound, position));
        }

        public void PlayRandomSFX(Sound sound)
        {
            int randomIndex = UnityEngine.Random.Range(0, _randomSoundBank[sound].clips.Length);
            // TODO: �٨S����
        }


        private IEnumerator PlayOnce(Sound sound, float preventTime = 0.1f)
        {
            _sfxPlayer.clip = null;
            yield return new WaitForSeconds(_soundBank[sound].delay);
            if (_isPreventPlayback)
                yield break;

            _sfxPlayer.PlayOneShot(_soundBank[sound].clip, _soundBank[sound].volume);
            if (preventTime > 0)
                _prevenPlayback = StartCoroutine(PreventPlaybackTime(preventTime));
        }

        private IEnumerator PlayInScene(Sound sound, Vector3 pos)
        {
            yield return new WaitForSeconds(_soundBank[sound].delay);
            AudioSource.PlayClipAtPoint(_soundBank[sound].clip, pos, _soundBank[sound].volume);
            yield break;
        }

        #endregion

        IEnumerator PreventPlaybackTime(float time)
        {
            _isPreventPlayback = true;
            yield return new WaitForSeconds(time);
            _isPreventPlayback = false;
        }
    } 



    public enum Sound
    {
        None
    }

    public enum Music
    {
        None,
        MusicA,
        MusicB
    }


    [System.Serializable]
    public struct SoundLibrary
    {
        public AudioClip clip;
        public Sound sound;
        [Range(0f, 1f)] public float volume;
        public float delay;
        public float startPosition;

    }

    [System.Serializable]
    public struct RandomSoundLibrary
    {
        public AudioClip[] clips;
        public Sound sound;
        [Range(0f, 1f)] public float volume;
    }

    [System.Serializable]
    public struct MusicLibrary
    {
        public AudioClip audioClip;
        public Music music;
        [Range(0f, 1f)] public float volume;
        public float startPosition;
        //[MinMaxSlider(0f,1f)] public Vector2 fade;
        public float fadeIn;
        public float fadeOut;
        public bool loop;
    }

    public enum Transition
    {
        Immediate,
        FadeOutThenFadeIn,
        OnlyFadeInNew,
        OnlyFadeOutCurrent,
        CrossFade
    }
    public enum FadeMode
    {
        None,
        Immediate,
        Fade,
        Cross
    }
}

