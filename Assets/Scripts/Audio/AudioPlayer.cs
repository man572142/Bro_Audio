using MiProduction.BroAudio.Library;
using MiProduction.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.AudioExtension;
using static UnityEngine.Networking.UnityWebRequest;

namespace MiProduction.BroAudio.Core
{
    [RequireComponent(typeof(AudioSource))]
	public abstract class AudioPlayer : MonoBehaviour
	{
        [SerializeField] protected AudioSource AudioSource = null;
        [SerializeField] protected AudioMixer AudioMixer;
        private string _volParaName = string.Empty;

        private Coroutine _subVolumeControl;
        
        public float MixerVolume
        {
            get
            {
                return MixerDecibelVolume.ToNormalizeVolume();
            }
            protected set
            {
                MixerDecibelVolume = value.ToDecibel();
            }
        }

        private float _mixerDecibelVolume = -1;
        protected float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume < 0)
                {
                    if (AudioMixer.GetFloat(_volParaName, out float currentVol))
                    {
                        //Debug.Log(currentVol);
                        _mixerDecibelVolume = currentVol;
                    }
                    else
                    {
                        Debug.LogError("[SoundSystem] Can't get exposed parameter in audio mixer,AudioMixerGroup's name and ExposedParameter's name should be the same");
                    }
                }
                
                return _mixerDecibelVolume;
            }
            set
            {
                if(_isControllingSubVolume)
                {
                    _mixerDecibelVolume = value.ClampDecibel();
                    AudioMixer.SetFloat(_volParaName, _mixerDecibelVolume);
                }
                else
                {
                    float result = (value.ToNormalizeVolume() * _subVolume).ToDecibel();
                    _mixerDecibelVolume = result.ClampDecibel();
                    AudioMixer.SetFloat(_volParaName, _mixerDecibelVolume);
                }
                //float result = (_mixerDecibelVolume.ToNormalizeVolume() * _subVolume).ToDecibel();
                //Debug.Log($"value:{value.ToNormalizeVolume()},result:{result},sub:{_subVolume}");
                //_mixerDecibelVolume = Mathf.Clamp(result, MinDecibelVolume, MaxDecibelVolume);
                //AudioMixer.SetFloat(_volParaName, _mixerDecibelVolume);
                
            }
        }

        private float _subVolume = 1f;
        private bool _isControllingSubVolume = false;


        public abstract bool IsPlaying { get; protected set; }
        public abstract bool IsStoping { get; protected set; }
        public abstract bool IsFadingOut { get; protected set; }
        public abstract bool IsFadingIn { get; protected set; }

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
        }

        

        public void SetVolume(float vol, float fadeTime)
        {
            // 只動SubVolume，使原本的音量以及FadeIn/Out以及此處音量能共同運作
            _subVolumeControl.Stop(this);
            _subVolumeControl = StartCoroutine(SubVolumeControl(vol, fadeTime));
        }

        private IEnumerator SubVolumeControl(float target, float fadeTime)
        {
            float start = _subVolume;
            float t = 0f;
            while (t < 1f)
            {
                //Debug.Log(_subVolume);
                _subVolume = Mathf.Lerp(start, target, t);
                t += Time.deltaTime / fadeTime;
                if (!IsFadingIn && !IsFadingOut)
                {

                }
                yield return null;
            }
            _subVolume = target;
        }
    } 
}
