using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.AudioExtension;
using static MiProduction.Extension.AnimationExtension;

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

        protected enum Command
        {
            None,
            Play,
            Stop,
        }
        public event Action<AudioPlayer> OnRecycle;

        public const float UseClipFadeSetting = -1f;
        public const float Immediate = 0f;
        public const float DefaultClipVolume = 0f;
        public const float DefaultTrackVolume = 1f;
        public const float DefaultMixerDecibelVolume = int.MinValue;
        private const string ExclusiveModeParaName = "_Send";

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;

        protected Coroutine PlaybackControlCoroutine;
        protected Coroutine FadeControlCoroutine;
        protected Coroutine TrackVolumeControlCoroutine;
        protected Coroutine RecycleCoroutine;

        protected BroAudioClip CurrentClip;
        protected Command LatestCommand = Command.None;

        private string _volParaName = string.Empty;
        

        // ClipVolume : 播放Clip的音量(0~1)，依不同的Clip有不同設定，而FadeIn/FadeOut也只作用在此值
        // TrackVolume : 音軌的音量(0~1)，也可算是此Player的音量，作用相當於混音的Fader
        // MixerDecibelVolume 實際在AudioMixer上的分貝數

        private float _clipVolume = DefaultClipVolume;
        public float ClipVolume
        {
            get => _clipVolume;
            protected set
            {
                _clipVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        private float _trackVolume = DefaultTrackVolume;
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }
        
        private float _mixerDecibelVolume = DefaultMixerDecibelVolume;
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume == DefaultMixerDecibelVolume)
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
        public bool IsStopping { get; protected set; }
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
            
            OnStopTweakingMainTrack += StopTweakingMainTrack;
        }

		protected virtual void Start()
		{
		}

        protected virtual void OnDestroy()
        {
            OnStopTweakingMainTrack -= StopTweakingMainTrack;
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

        public virtual void Play(int id, BroAudioClip clip, PlaybackPreference pref)
        {
            LatestCommand = Command.Play;
            ID = id;
            CurrentClip = clip;
            // Reset the volume of the AudioTrack that has been assigned on this Player
            ClipVolume = 0f;

            IsStopping = false;
            this.SafeStopCoroutine(RecycleCoroutine);

            ExecuteAfterChainingMethod(() => 
            {
                if(LatestCommand == Command.Play)
                {
                    this.SafeStopCoroutine(FadeControlCoroutine);
                    this.StartCoroutineAndReassign(PlayControl(clip, pref), ref PlaybackControlCoroutine);
                }
                else
                {
                    EndPlaying();
                }
            });
        }

        private void ExecuteAfterChainingMethod(Action action)
        {
            AsyncTaskExtension.DelayDoAction(AsyncTaskExtension.MillisecondInSeconds,action);
        }


        private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference setting)
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
                if (HasFading(clip.FadeIn,setting.FadeIn,out float fadeIn))
                {
                    IsFadingIn = true;
                    yield return this.StartCoroutineAndReassign(Fade(clip.Volume, fadeIn, fader), ref FadeControlCoroutine);
                    IsFadingIn = false;
                }
                else
                {
                    ClipVolume = clip.Volume;
                }
                #endregion

                #region FadeOut
                if (HasFading(clip.FadeOut,setting.FadeOut,out float fadeOut))
                {
                    yield return new WaitUntil(() => (endTime - AudioSource.time) <= clip.FadeOut);
                    IsFadingOut = true;
                    yield return this.StartCoroutineAndReassign(Fade(0f, fadeOut, fader),ref FadeControlCoroutine) ;
                    IsFadingOut = false;
                }
                else
                {
                    yield return new WaitUntil(() => AudioSource.time >= endTime);
                }
                #endregion
            } while (setting.IsLoop);

            EndPlaying();
        }

        public virtual void Stop(float fadeOut,Action onFinishStopping)
        {
            if (IsStopping && fadeOut != Immediate)
            {
                return;
            }
            LatestCommand = Command.Stop;
            this.SafeStopCoroutine(FadeControlCoroutine);
            this.StartCoroutineAndReassign(StopControl(onFinishStopping, fadeOut), ref PlaybackControlCoroutine);
        }
		#region Stop Overloads
		public virtual void Stop() => Stop(UseClipFadeSetting);
        public virtual void Stop(float fadeOut) => Stop(fadeOut, null);
        public virtual void Stop(Action onFinishStopping) => Stop(UseClipFadeSetting, onFinishStopping);
		#endregion

		private IEnumerator StopControl(Action onFinishStopping,float overrideFade)
        {
            if (ID <= 0 || !IsPlaying)
            {
                onFinishStopping?.Invoke();
                yield break;
            }

            IsStopping = true;
            if (HasFading(CurrentClip.FadeOut,overrideFade,out float fadeTime))
            {
                if (IsFadingOut)
                {
                    // 如果已經在FadeOut了(也就是剛剛的Play已快結束)，就等它FadeOut不強制停止
                    float endTime = AudioSource.clip.length - CurrentClip.EndPosition;
                    yield return new WaitUntil(() => AudioSource.time >= endTime);
                }
                else
                {
                    yield return this.StartCoroutineAndReassign(Fade(0f, fadeTime, VolumeControl.Clip), ref FadeControlCoroutine);
                }
            }
            EndPlaying();
            onFinishStopping?.Invoke();
        }

        private bool HasFading(float clipFade,float overrideFade,out float fadeTime)
		{
            fadeTime = clipFade;
            if(overrideFade != UseClipFadeSetting)
			{
                fadeTime = overrideFade;
			}
            return fadeTime > Immediate;
		}

        private void EndPlaying()
        {
            ID = -1;
            _clipVolume = DefaultClipVolume;
            _trackVolume = DefaultTrackVolume;
            _mixerDecibelVolume = DefaultMixerDecibelVolume;

            AudioSource.Stop();
            AudioSource.clip = null;
            CurrentClip = null;
            IsPlaying = false;
            IsStopping = false;
            LatestCommand = Command.None;
            this.StartCoroutineAndReassign(Recycle(),ref RecycleCoroutine);
            this.SafeStopCoroutine(TrackVolumeControlCoroutine);
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

        protected IEnumerator Recycle()
        {
            yield return null;
            MixerDecibelVolume = AudioConstant.MinDecibelVolume;
            OnRecycle?.Invoke(this);
        }
	}
}
