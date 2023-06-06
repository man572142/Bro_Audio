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

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>
	{
        protected enum VolumeControl
		{
            Clip,
            Track,
            MixerDecibel,
		}
        public event Action<AudioPlayer> OnRecycle;

        private const string ExclusiveModeParaName = "_Send";

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;

        protected Coroutine PlaybackControlCoroutine;
        protected Coroutine FadeControlCoroutine;
        protected Coroutine TrackVolumeControlCoroutine;
        protected Coroutine RecycleCoroutine;

        protected BroAudioClip CurrentClip;

        private string _volParaName = string.Empty;
        

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
                string valueString = value == null ? "null" : value.name;
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
            // Reset the volume of the AudioTrack that has been assigned on this Player
            ClipVolume = 0f;

            IsStoping = false;
            this.SafeStopCoroutine(RecycleCoroutine);

            // Delay 1ms to allow the chaining method to execute before playing
            AsyncTaskExtension.DelayDoAction(AsyncTaskExtension.MillisecondInSeconds, () =>
            {
                // TODO: 如果已經IsPlaying，則重播?
                this.SafeStopCoroutine(FadeControlCoroutine);
                this.StartCoroutineAndReassign(PlayControl(clip, pref, onFinishPlaying),ref PlaybackControlCoroutine);
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
                // TODO: 除非想要立即停止
				return;
			}
            this.SafeStopCoroutine(FadeControlCoroutine);
            this.StartCoroutineAndReassign(StopControl(onFinishStopping), ref PlaybackControlCoroutine);
        }

		public IAudioPlayer SetVolume(float vol,float fadeTime)
        {
            // 只動TrackVolume
            this.SafeStopCoroutine(TrackVolumeControlCoroutine);
            TrackVolumeControlCoroutine = StartCoroutine(Fade(vol, fadeTime,VolumeControl.Track));
            return this;
        }


        protected IEnumerator Fade(float targetVol,float duration,VolumeControl fader)
        {
            Func<float> GetVol = null;
            Action<float> SetVol = null;

			switch (fader)
			{
				case VolumeControl.Clip:
                    GetVol = () => ClipVolume;
                    SetVol = (vol) => ClipVolume = vol; 
					break;
				case VolumeControl.Track:
                    GetVol = () => TrackVolume;
                    SetVol = (vol) => TrackVolume = vol;
					break;
				case VolumeControl.MixerDecibel:
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
            VolumeControl fader = VolumeControl.Clip;

            do
            {
                AudioSource.Stop();
                AudioSource.time = clip.StartPosition;
                AudioSource.Play();
                IsPlaying = true;

                #region FadeIn
                if (clip.FadeIn > 0)
                {
                    IsFadingIn = true;
                    yield return this.StartCoroutineAndReassign(Fade(clip.Volume, clip.FadeIn, fader), ref FadeControlCoroutine);
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
                    yield return this.StartCoroutineAndReassign(Fade(0f, clip.FadeOut, fader),ref FadeControlCoroutine) ;
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
                    yield return this.StartCoroutineAndReassign(Fade(0f, CurrentClip.FadeOut, VolumeControl.Clip), ref FadeControlCoroutine);
                }
            }
            EndPlaying();
            onFinishStopping?.Invoke();
        }

        private void EndPlaying()
        {
            ID = -1;
            //StopAllTransportControl();
            ClipVolume = 0f;
            AudioSource.Stop();
            AudioSource.clip = null;
            AudioSource.loop = false;
            IsPlaying = false;
            IsStoping = false;
            this.StartCoroutineAndReassign(Recycle(),ref RecycleCoroutine);
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
            this.SafeStopCoroutine(TrackVolumeControlCoroutine);
            _clipVolume = 1f;
            _trackVolume = 1f;
            _mixerDecibelVolume = -1f;
        }

        protected IEnumerator Recycle()
        {
            yield return null;
            OnRecycle?.Invoke(this);
        }
	}
}
