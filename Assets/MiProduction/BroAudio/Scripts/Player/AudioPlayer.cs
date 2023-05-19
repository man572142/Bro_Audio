using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.AudioExtension;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        public event Action<AudioPlayer> OnRecycle;

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;

        protected Coroutine PlayControlCoroutine;
        protected Coroutine StopControlCoroutine;
        protected BroAudioClip CurrentClip;

        private string _volParaName = string.Empty;
        private Coroutine _subVolumeControl;

        // ClipVolume : 播放Clip的音量(0~1)，依不同的Clip有不同設定，而FadeIn/FadeOut也只作用在此值
        // TrackVolume : 音軌的音量(0~1)，也可算是此Player的音量，作用相當於混音的Fader
        // MixerDecibelVolume 實際在AudioMixer上的分貝數

        private float _clipVolume = 1f;
        public float ClipVolume
        {
            get => _clipVolume;
            protected set
            {
                _clipVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        private float _trackVolume = 1f;
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }
        
        private float _mixerDecibelVolume = -1;
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume < 0)
                {
                    if (AudioMixer.GetFloat(_volParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                    else
                    {
                        LogError("Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel();
                AudioMixer.SetFloat(_volParaName, _mixerDecibelVolume);
            }
        }

        public AudioMixerGroup AudioTrack
		{
            get => AudioSource.outputAudioMixerGroup;
			set
			{
                if(value != null)
				{
                    _volParaName = value.name;
                    AudioSource.outputAudioMixerGroup = value;
                }
                else
				{
                    _volParaName = string.Empty;
                    AudioSource.outputAudioMixerGroup = null;
                }
			}
		}

        public bool IsPlaying { get; protected set; }
        public bool IsStoping { get; protected set; }
        public bool IsFadingOut { get; protected set; }
        public bool IsFadingIn { get; protected set; }
		public int ID { get; protected set; }

		protected virtual void Awake()
        {
            if (AudioSource == null)
            {
                AudioSource = GetComponent<AudioSource>();
            }
            if (AudioMixer == null)
            {
                AudioMixer = AudioSource.outputAudioMixerGroup.audioMixer;
            }

            OnStandOut += StandOutHandler;
            OnLowPassOthers += LowPassHandler;
            OnRecycle += ResetValue;
        }

        protected virtual void Start()
		{

		}

		public virtual void Play(int id,BroAudioClip clip, PlaybackPreference pref, Action onFinishPlaying = null)
        {
            ID = id;
            CurrentClip = clip;
            PlayControlCoroutine = StartCoroutine(PlayControl(clip, pref, onFinishPlaying));
        }

        public virtual void Stop()
		{
            Stop(null);
		}

        public virtual void Stop(Action onFinishStopping)
        {
            if (IsStoping)
            {
                LogWarning("The music player is already processing StopMusic !");
                return;
            }
            StopControlCoroutine = StartCoroutine(StopControl(onFinishStopping));
        }

		public IAudioPlayer SetVolume(float vol,float fadeTime)
        {
            // 只動TrackVolume
            _subVolumeControl.StopIn(this);
            _subVolumeControl = StartCoroutine(SetTrackVolume(vol, fadeTime));
            return this;
        }

        private IEnumerator SetTrackVolume(float target, float fadeTime)
        {
            float start = TrackVolume;
            float t = 0f;
            while (t < 1f)
            {
                TrackVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                yield return null;
            }
            TrackVolume = target;
        }


        protected IEnumerator Fade(float duration, float targetVolume)
        {
            float currentTime = 0;
            float currentVol = ClipVolume;
            float targetValue = Mathf.Clamp(targetVolume, MinVolume, MaxVolume);
            Ease ease = currentVol < targetValue ? SoundManager.FadeInEase : SoundManager.FadeOutEase;
            float newVol = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                newVol = Mathf.Lerp(currentVol, targetValue, (currentTime / duration).SetEase(ease));
                ClipVolume = newVol;
                yield return null;
            }
            yield break;
        }

        private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference setting, Action onFinishPlaying)
        {
            yield return new WaitForSeconds(setting.Delay);
            ClipVolume = 0f;

            AudioSource.clip = clip.AudioClip;
            AudioSource.time = clip.StartPosition;
            float endTime = AudioSource.clip.length - clip.EndPosition;
            AudioSource.loop = setting.IsLoop;
            AudioSource.Play();
            IsPlaying = true;

            do
            {
                #region FadeIn
                if (clip.FadeIn > 0)
                {
                    IsFadingIn = true;
                    yield return StartCoroutine(Fade(clip.FadeIn, clip.Volume));
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = clip.Volume;
                }
                #endregion

                #region FadeOut
                if (clip.FadeOut > 0)
                {
                    yield return new WaitUntil(() => (endTime - AudioSource.time) <= clip.FadeOut);
                    IsFadingOut = true;
                    yield return StartCoroutine(Fade(clip.FadeOut, 0f));
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => endTime <= AudioSource.time);
                }
                #endregion
            } while (setting.IsLoop);

            EndPlaying();
            onFinishPlaying?.Invoke();
        }

        private IEnumerator StopControl(Action onFinishStopping)
        {
            if (ID <= 0 || !IsPlaying)
            {
                EndPlaying();
                onFinishStopping?.Invoke();
                yield break;
            }

            IsStoping = true;
            if (CurrentClip.FadeOut > 0)
            {
                if (IsFadingOut)
                {
                    // 目前設計成:如果原有的聲音已經在FadeOut了，就等它FadeOut不強制停止
                    AudioSource.loop = false;
                    yield return new WaitUntil(() => AudioSource.clip.length == AudioSource.time);
                }
                else
                {
                    PlayControlCoroutine.StopIn(this);
                    yield return StartCoroutine(Fade(CurrentClip.FadeOut, 0f));
                }
            }
            EndPlaying();
            onFinishStopping?.Invoke();
        }

        private void EndPlaying()
        {
            ID = -1;
            PlayControlCoroutine.StopIn(this);
            ClipVolume = 0f;
            AudioSource.Stop();
            AudioSource.clip = null;
            AudioSource.loop = false;
            IsPlaying = false;
            IsStoping = false;
            Recycle();
        }

        private void ResetValue(AudioPlayer player)
        {
            ID = -1;
            _subVolumeControl.StopIn(this);
            _clipVolume = 1f;
            _trackVolume = 1f;
            _mixerDecibelVolume = -1f;
        }

        protected void Recycle()
        {
            OnRecycle?.Invoke(this);
        }

        private void OnDestroy()
		{
            OnStandOut -= StandOutHandler;
            OnLowPassOthers += LowPassHandler;
        }

    }

	#region Stands Out
	[RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer,IRecyclable<AudioPlayer>
    {
        // All Audio Player will subscribe this static event
        private static event Action<float, float, AudioPlayer> OnStandOut;

        public IAudioPlayer StandsOut(float standoutRatio, float fadeTime = 0.5f)
        {
            if (standoutRatio < 0 || standoutRatio > 1)
            {
                LogError("Stand out volume ratio should be between 0 and 1");
                return null;
            }

            OnStandOut?.Invoke(standoutRatio, fadeTime, this);
            return this;
        }

        private void StandOutHandler(float standoutRatio, float fadeTime, AudioPlayer standoutPlayer)
        {
            // 要再控制Coroutine?
            if (standoutPlayer == this)
            {
                StartCoroutine(StandsOutControl(standoutRatio, fadeTime, standoutPlayer));
            }
            else
            {
                StartCoroutine(StandsOutControl(1 - standoutRatio, fadeTime, standoutPlayer));
            }
        }

        private IEnumerator StandsOutControl(float vol, float fadeTime, AudioPlayer standoutPlayer)
        {
            float origin = TrackVolume;

            SetVolume(vol, fadeTime);

            yield return _subVolumeControl;
            yield return new WaitWhile(() => standoutPlayer.IsPlaying);

            SetVolume(origin, fadeTime);
        }
    }
    #endregion


    #region LowPass Other
    [RequireComponent(typeof(AudioSource))]
    public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IRecyclable<AudioPlayer>
    {
        public const string LowPassExposedName = "_LowPass";
        public const float DefaultLowPassFrequence = 300f;
        private static event Action<float,float,AudioPlayer> OnLowPassOthers;

        public IAudioPlayer LowPassOthers(float freq,float fadeTime)
        {
            OnLowPassOthers?.Invoke(freq,fadeTime,this);
            return this;
        }

        public void LowPassHandler(float freq, float fadeTime,AudioPlayer player)
		{
            if(player != this)
			{
                AudioMixer.SetFloat(_volParaName + LowPassExposedName, freq < 0? DefaultLowPassFrequence: freq);
            }
		}
    }
    #endregion
}
