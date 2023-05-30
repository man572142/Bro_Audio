using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.AudioExtension;
using static MiProduction.Extension.AnimationExtension;
using static MiProduction.BroAudio.Utility;
using System.Threading.Tasks;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        protected enum Fader
		{
            Clip,
            Track,
            MixerDecibel,
		}

        public event Action<AudioPlayer> OnRecycle;

        private const string ExclusiveModeParaName = "_Send";

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;

        protected Coroutine PlayControlCoroutine;
        protected Coroutine StopControlCoroutine;
        protected BroAudioClip CurrentClip;

        private string _volParaName = string.Empty;
        private Coroutine _trackVolumeControl;

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
                _volParaName = value != null ? value.name : string.Empty;
                AudioSource.outputAudioMixerGroup = value;
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
                //TODO: should warn the user . don't do auto assigning because outputAudioMixerGroup is null at the time
                AudioMixer = AudioSource.outputAudioMixerGroup.audioMixer;
            }
            
            OnRecycle += ResetValue;
            OnStopTweakingMainTrack += StopTweakingMainTrack;
        }

		protected virtual void Start()
		{

		}

        protected virtual void OnDestroy()
        {
            OnRecycle -= ResetValue;
            OnStopTweakingMainTrack -= StopTweakingMainTrack;
        }

        public virtual void Play(int id,BroAudioClip clip, PlaybackPreference pref, Action onFinishPlaying = null)
        {
            ID = id;
            CurrentClip = clip;
            // Reset the Track's volume to default
            ClipVolume = 0f;
            // Delay 1ms to allow the chaining method to execute first
            AsyncTaskExtension.DelayDoAction(AsyncTaskExtension.MillisecondInSeconds, () =>
            {
                 PlayControlCoroutine = StartCoroutine(PlayControl(clip, pref, onFinishPlaying));
            });
        }

        public virtual void Stop()
		{
            Stop(null);
		}

        public virtual void Stop(Action onFinishStopping)
        {
            if (IsStoping)
            {
                LogWarning("The AudioPlayer is already processing Stop.");
                return;
            }
            StopControlCoroutine = StartCoroutine(StopControl(onFinishStopping));
        }

		public IAudioPlayer SetVolume(float vol,float fadeTime)
        {
            // 只動TrackVolume
            _trackVolumeControl.StopIn(this);
            _trackVolumeControl = StartCoroutine(Fade(vol, fadeTime,Fader.Track));
            return this;
        }


        protected IEnumerator Fade(float targetVol,float duration,Fader fader)
        {
            Func<float> GetVol = null;
            Action<float> SetVol = null;

			switch (fader)
			{
				case Fader.Clip:
                    GetVol = () => ClipVolume;
                    SetVol = (vol) => ClipVolume = vol; 
					break;
				case Fader.Track:
                    GetVol = () => TrackVolume;
                    SetVol = (vol) => TrackVolume = vol;
					break;
				case Fader.MixerDecibel:
					break;
			}

			float startVol = GetVol();
            Ease ease = startVol < targetVol ? SoundManager.FadeInEase : SoundManager.FadeOutEase;

            IEnumerable<float> volumes = GetLerpValuesPerFrame(startVol, targetVol, duration, ease);
            if(volumes != null)
			{
                foreach(float vol in volumes)
				{
                    SetVol(vol);
                    yield return null;
				}
			}
		}

		private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference setting, Action onFinishPlaying)
        {
            if(setting.Delay > 0)
			{
                yield return new WaitForSeconds(setting.Delay);
            }
            //Don't Remove this ! It needs to reset the Exclusive track if there is a chain method. The same line in Play() only reset the Track .They are both necessary
            ClipVolume = 0f;

            AudioSource.clip = clip.AudioClip;
            float endTime = AudioSource.clip.length - clip.EndPosition;
            IsPlaying = true;
            Fader fader = Fader.Clip;

            do
            {
                AudioSource.Stop();
                AudioSource.time = clip.StartPosition;
                AudioSource.Play();

                #region FadeIn
                if (clip.FadeIn > 0)
                {
                    IsFadingIn = true;
                    yield return StartCoroutine(Fade(clip.Volume, clip.FadeIn, fader));
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
                    yield return StartCoroutine(Fade(0f, clip.FadeOut, fader));
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
                    yield return StartCoroutine(Fade(0f, CurrentClip.FadeOut,Fader.Clip));
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

        private void SetToExclusiveMode()
		{
            if(!string.IsNullOrEmpty(_volParaName))
			{
                _volParaName += ExclusiveModeParaName;
            }
            else
			{
                // TODO : add log
                Debug.LogError("");
			}
		}

        private void ResetValue(AudioPlayer player)
        {
            ID = -1;
            _trackVolumeControl.StopIn(this);
            _clipVolume = 1f;
            _trackVolume = 1f;
            _mixerDecibelVolume = -1f;
        }

        protected void Recycle()
        {
            OnRecycle?.Invoke(this);
        }
	}
}
