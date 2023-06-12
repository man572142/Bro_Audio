using UnityEngine;
using UnityEngine.Audio;
using MiProduction.Extension;
using static MiProduction.Extension.AudioExtension;

namespace MiProduction.BroAudio.Runtime
{
    [RequireComponent(typeof(AudioSource))]
	public partial class AudioPlayer : MonoBehaviour,IAudioPlayer,IRecyclable<AudioPlayer>,IPlaybackControllable
	{
        public const float DefaultClipVolume = 0f;
        public const float DefaultTrackVolume = 1f;
        public const float DefaultMixerDecibelVolume = int.MinValue;

        protected string VolumeParaName = string.Empty;
        protected float TrackVolumeBeforeMute = DefaultTrackVolume;

        private float _clipVolume = DefaultClipVolume;
        private float _trackVolume = DefaultTrackVolume;
        private float _mixerDecibelVolume = DefaultMixerDecibelVolume;

        /// <summary>
        /// 播放Clip的音量(0~1)，依不同的Clip有不同設定，而FadeIn/FadeOut也只作用在此值
        /// </summary>
        public float ClipVolume
        {
            get => _clipVolume;
            protected set
            {
                _clipVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        /// <summary>
        /// 音軌的音量(0~1)，也可算是此Player的音量，作用相當於混音的Fader
        /// </summary>
        public float TrackVolume
        {
            get => _trackVolume;
            private set
            {
                _trackVolume = value;
                MixerDecibelVolume = (_clipVolume * _trackVolume).ToDecibel();
            }
        }

        /// <summary>
        /// MixerDecibelVolume 實際在AudioMixer上的分貝數
        /// </summary>
        public float MixerDecibelVolume
        {
            get
            {
                if(_mixerDecibelVolume == DefaultMixerDecibelVolume)
                {
                    if (AudioMixer.GetFloat(VolumeParaName, out float currentVol))
                    {
                        _mixerDecibelVolume = currentVol;
                    }
                }
                
                return _mixerDecibelVolume;
            }
            private set
            {
                _mixerDecibelVolume = value.ClampDecibel();
                AudioMixer.SetFloat(VolumeParaName, _mixerDecibelVolume);
            }
        }

        IAudioPlayer IVolumeSettable.SetVolume(float vol, float fadeTime)
        {
            // 只動TrackVolume
            this.SafeStopCoroutine(_trackVolumeControlCoroutine);
            _trackVolumeControlCoroutine = StartCoroutine(Fade(vol, fadeTime, VolumeControl.Track));
            return this;
        }
    }
}
