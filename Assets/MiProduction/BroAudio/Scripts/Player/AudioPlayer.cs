using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.AudioExtension;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
	public abstract partial class AudioPlayer : MonoBehaviour,IAudioPlayer
	{

        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;
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

        public abstract bool IsPlaying { get; protected set; }
        public abstract bool IsStoping { get; protected set; }
        public abstract bool IsFadingOut { get; protected set; }
        public abstract bool IsFadingIn { get; protected set; }

        public abstract void Stop(float fadeTime);

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
            _volParaName = AudioSource.outputAudioMixerGroup.name;

            OnStandOut += StandOutHandler;
            OnLowPassOthers += LowPassHandler;
        }

        public void SetVolume(float vol,float fadeTime)
        {
            // 只動TrackVolume
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(SetTrackVolume(vol, fadeTime));
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

		private void OnDestroy()
		{
            OnStandOut -= StandOutHandler;
		}
	}

	#region Stands Out
	[RequireComponent(typeof(AudioSource))]
    public abstract partial class AudioPlayer : MonoBehaviour, IAudioPlayer
    {
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
    public abstract partial class AudioPlayer : MonoBehaviour, IAudioPlayer
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
