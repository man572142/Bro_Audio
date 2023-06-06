using System;
using System.Collections;
using System.Collections.Generic;
using MiProduction.Extension;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.CoroutineExtension;

namespace MiProduction.BroAudio.Runtime
{
	public partial class AudioPlayer : MonoBehaviour, IAudioPlayer, IRecyclable<AudioPlayer>
	{
        public struct Parameter
		{
            public float Start;
            public float Target;
            public float FadeTime;
            public ExposedParameter Type;
		}

        public static event Action OnStopTweakingMainTrack = null;

        private Coroutine _mainTrackTweaking;

        private void StopTweakingMainTrack()
        {
            this.SafeStopCoroutine(_mainTrackTweaking);
        }

        /// <summary>
        /// �ϰ��F��Player�H�~����LPlayer���ܫ��w��v�����q�A���켽�񧹲�����C
        /// </summary>
        /// <param name="othersVol">�ȶ�����0~1����</param>
        /// <param name="fadeTime"></param>
        /// <returns></returns>
        public IAudioPlayer DuckOthers(float othersVol, float fadeTime)
        {
            if (othersVol <= 0f || othersVol > 1f)
            {
                LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
                return this;
            }

            SetToExclusiveMode();

            float targetVolInDb = AudioExtension.ToDecibel(othersVol);
            Parameter parameter = new Parameter()
			{
				Start = 0f,
				Target = targetVolInDb,
				FadeTime = fadeTime,
				Type = ExposedParameter.Volume
			};

            OnStopTweakingMainTrack?.Invoke();
            _mainTrackTweaking = StartCoroutine(TweakMainTrackParameter(parameter));

            return this;
        }

        /// <summary>
        /// �ϰ��F��Player�H�~��Player���ϥΧC�q�ĪG���A���켽�񧹲�����
        /// </summary>
        /// <param name="fadeTime"></param>
        /// <param name="freq"></param>
        /// <returns></returns>
        public IAudioPlayer LowPassOthers(float fadeTime, float freq = 300f)
        {
            if (!IsValidFrequence(freq))
            {
                return this;
            }

            SetToExclusiveMode();

            Parameter parameter = new Parameter()
            {
				Start = AudioConstant.MaxFrequence,
				Target = freq,
				FadeTime = fadeTime,
				Type = ExposedParameter.LowPass
			};

            OnStopTweakingMainTrack?.Invoke();
            _mainTrackTweaking = StartCoroutine(TweakMainTrackParameter(parameter));

            return this;
        }

        /// <summary>
        /// �ϰ��F��Player�H�~��Player���ϥΰ��q�ĪG���A���켽�񧹲�����
        /// </summary>
        /// <param name="fadeTime"></param>
        /// <param name="freq"></param>
        /// <returns></returns>
        public IAudioPlayer HighPassOthers(float fadeTime, float freq = 2000f)
		{
            if(!IsValidFrequence(freq))
			{
                return this;
			}

			SetToExclusiveMode();

			Parameter parameter = new Parameter()
			{
				Start = AudioConstant.MinFrequence,
				Target = freq,
				FadeTime = fadeTime,
				Type = ExposedParameter.LowPass
			};

			OnStopTweakingMainTrack?.Invoke();
			_mainTrackTweaking = StartCoroutine(TweakMainTrackParameter(parameter));

			return this;
		}

		private static bool IsValidFrequence(float freq)
		{
			if (freq < AudioConstant.MinFrequence || freq > AudioConstant.MaxFrequence)
			{
				LogWarning($"LowPass frequence should be in {AudioConstant.MinFrequence}Hz ~ {AudioConstant.MaxFrequence}Hz.");
                return false;
			}
            return true;
		}

		private IEnumerator TweakMainTrackParameter(Parameter para)
        {
            yield return SoundManager.Instance.SetMainGroupTrackParameter(para.Type, para.Target, para.FadeTime);
            if(IsPlaying)
			{
                yield return new WaitWhile(() => IsPlaying);
            }
            yield return SoundManager.Instance.SetMainGroupTrackParameter(para.Type, para.Start, para.FadeTime);
        }
    }

}