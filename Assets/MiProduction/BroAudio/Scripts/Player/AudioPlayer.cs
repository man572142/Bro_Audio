using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using MiProduction.BroAudio.Data;
using static MiProduction.Extension.AnimationExtension;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        public static Dictionary<int, IPlaybackControllable> ResumablePlayers = null;

        public event Action<AudioPlayer> OnRecycle;
        public event Action<PlaybackPreference> DecoratePlaybackPreference;

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;

        // TODO : Don't use instance
        protected BroAudioClip CurrentClip;

        private Command _latestCommand = Command.None;
        private StopMode _stopMode = default;
        private Coroutine _playbackControlCoroutine;
        private Coroutine _fadeControlCoroutine;
        private Coroutine _trackVolumeControlCoroutine;
        private Coroutine _recycleCoroutine;

        public AudioMixerGroup AudioTrack
		{
            get => AudioSource.outputAudioMixerGroup;
			set
			{
                VolumeParaName = value != null ? value.name : string.Empty;
                AudioSource.outputAudioMixerGroup = value;
            }
		}

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

        public void Play(int id, BroAudioClip clip, PlaybackPreference pref)
        {
            _latestCommand = Command.Play;
            ID = id;
            CurrentClip = clip;

            IsStopping = false;
            this.SafeStopCoroutine(_recycleCoroutine);

            ExecuteAfterChainingMethod(() => 
            {
                if(_latestCommand == Command.Play)
                {
                    DecoratePlaybackPreference?.Invoke(pref);
                    this.SafeStopCoroutine(_fadeControlCoroutine);
                    this.StartCoroutineAndReassign(PlayControl(clip, pref), ref _playbackControlCoroutine);
                }
                else
                {
                    EndPlaying();
                }
            });

            void ExecuteAfterChainingMethod(Action action)
            {
                AsyncTaskExtension.DelayDoAction(AsyncTaskExtension.MillisecondInSeconds, action);
            }
        }

        private IEnumerator PlayControl(BroAudioClip clip, PlaybackPreference pref)
        {
            if(pref.HaveToWaitForPrevious)
			{
                yield return new WaitUntil(() => pref.HaveToWaitForPrevious == false);
			}

            if(pref.Delay > 0)
			{
                yield return new WaitForSeconds(pref.Delay);
            }

            AudioSource.clip = clip.AudioClip;
            float endTime = AudioSource.clip.length - clip.EndPosition;
            VolumeControl fader = VolumeControl.Clip;
            ClipVolume = 0f;
            IsPlaying = true;
            RemoveFromResumablePlayer();

            do
			{
                switch (_stopMode)
                {
                    case StopMode.Stop:
                        PlayFromPos(clip.StartPosition);
                        break;
                    case StopMode.Pause:
                        AudioSource.UnPause();
                        break;
                    case StopMode.Mute:
                        //TODO: 應該回到原本音量而不是1
                        this.SetVolume(TrackVolumeBeforeMute);
                        if (!AudioSource.isPlaying)
                        {
                            PlayFromPos(clip.StartPosition);
                        }
                        break;
                }
                _stopMode = default;

                #region FadeIn
                if (HasFading(clip.FadeIn, pref.FadeIn, out float fadeIn))
				{
					IsFadingIn = true;
					yield return this.StartCoroutineAndReassign(Fade(clip.Volume, fadeIn, fader), ref _fadeControlCoroutine);
					IsFadingIn = false;
				}
				else
				{
					ClipVolume = clip.Volume;
				}
				#endregion

				#region FadeOut
				if (HasFading(clip.FadeOut, pref.FadeOut, out float fadeOut))
				{
                    yield return new WaitUntil(() => (endTime - AudioSource.time) <= clip.FadeOut);
					IsFadingOut = true;
					yield return this.StartCoroutineAndReassign(Fade(0f, fadeOut, fader), ref _fadeControlCoroutine);
					IsFadingOut = false;
				}
				else
				{
					yield return new WaitUntil(() => AudioSource.time >= endTime);
				}
                #endregion
			} while (pref.IsLoop);

            EndPlaying();

			void PlayFromPos(float pos)
			{
				AudioSource.Stop();
				AudioSource.time = pos;
				AudioSource.Play();
			}
		}

		#region Stop Overloads
		public virtual void Stop() 
            => Stop(UseLibraryManagerSetting);
        public virtual void Stop(float fadeOut) 
            => Stop(fadeOut, null);
        public virtual void Stop(Action onFinishStopping) 
            => Stop(UseLibraryManagerSetting, onFinishStopping);
        public virtual void Stop(float fadeOut, Action onFinished) 
            => Stop(fadeOut, default, onFinished);
        #endregion
        public virtual void Stop(float overrideFade, StopMode stopMode,Action onFinished)
        {
            if (IsStopping && overrideFade != Immediate)
            {
                return;
            }
            _latestCommand = (Command)(stopMode + (int)Command.Stop);
            _stopMode = stopMode;
            this.SafeStopCoroutine(_fadeControlCoroutine);
            this.StartCoroutineAndReassign(StopControl(overrideFade, onFinished, stopMode), ref _playbackControlCoroutine);
        }

		private IEnumerator StopControl(float overrideFade, Action onFinished,StopMode stopMode)
        {
            if (ID <= 0 || !IsPlaying)
            {
                // TODO:與AudioSource的IsPlaying區別
                onFinished?.Invoke();
                yield break;
            }

            IsStopping = true;

			#region FadeOut
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
                    yield return this.StartCoroutineAndReassign(Fade(0f, fadeTime, VolumeControl.Clip), ref _fadeControlCoroutine);
                }
            }
			#endregion

			switch (stopMode)
			{
				case StopMode.Stop:
                    EndPlaying();
                    break;
				case StopMode.Pause:
                    AudioSource.Pause();
                    AddResumablePlayer();
                    break;
				case StopMode.Mute:
                    TrackVolumeBeforeMute = TrackVolume;
                    this.SetVolume(0f, 0f);
                    AddResumablePlayer();
                    break;
			}
            ResetPlaybackState();
            onFinished?.Invoke();
		}

        private bool HasFading(float clipFade, float overrideFade, out float fadeTime)
        {
            fadeTime = clipFade;
            if (overrideFade != UseLibraryManagerSetting)
            {
                fadeTime = overrideFade;
            }
            return fadeTime > Immediate;
        }

        private void AddResumablePlayer()
		{
            ResumablePlayers ??= new Dictionary<int, IPlaybackControllable>();
            ResumablePlayers[ID] = this;
        }

        private void RemoveFromResumablePlayer()
		{
            ResumablePlayers?.Remove(ID);
        }

        private void EndPlaying()
		{
			ID = -1;
			_clipVolume = DefaultClipVolume;
			_trackVolume = DefaultTrackVolume;
			_mixerDecibelVolume = DefaultMixerDecibelVolume;
            _stopMode = default;
            TrackVolumeBeforeMute = DefaultTrackVolume;

            AudioSource.Stop();
			AudioSource.clip = null;
			CurrentClip = null;
			ResetPlaybackState();
			this.StartCoroutineAndReassign(Recycle(), ref _recycleCoroutine);
			this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            RemoveFromResumablePlayer();
        }

		private void ResetPlaybackState()
		{
			IsPlaying = false;
			IsStopping = false;
			_latestCommand = Command.None;
		}

		public void SetToExclusiveMode()
		{
            if(!string.IsNullOrEmpty(VolumeParaName))
			{
                VolumeParaName += ExclusiveModeParaName;
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
