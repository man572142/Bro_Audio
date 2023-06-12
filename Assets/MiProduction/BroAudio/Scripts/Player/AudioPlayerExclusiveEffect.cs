using System;
using System.Collections;
using System.Collections.Generic;
using MiProduction.Extension;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.CoroutineExtension;

namespace MiProduction.BroAudio.Runtime
{
	public class AudioPlayerExclusiveEffect : AudioPlayerDecorator , IPlayerExclusive
	{
		public struct Parameter
		{
			public float Start;
			public float Target;
			public float FadeTime;
			public ExposedParameter Type;
		}

        // TODO: 可以跟SoundManager的整合
        private Dictionary<ExposedParameter, Coroutine> _mainTrackTweakingDict = null;

		public override void Init(AudioPlayer player)
		{
			base.Init(player);
            SoundManager.Instance.OnStopMainTrackTweaking += StopTweakingMainTrack;
        }

		protected override void Dispose(AudioPlayer player)
		{
            SoundManager.Instance.OnStopMainTrackTweaking -= StopTweakingMainTrack;
            base.Dispose(player);
		}

		IPlayerExclusive IPlayerExclusive.DuckOthers(float othersVol, float fadeTime)
		{
			if (othersVol <= 0f || othersVol > 1f)
			{
				LogWarning($"Stand out ratio should be less than 1 and greater than 0.");
				return this;
			}

			Player.SetToExclusiveMode();

			float targetVolInDb = AudioExtension.ToDecibel(othersVol);
			Parameter parameter = new Parameter()
			{
				Start = 0f,
				Target = targetVolInDb,
				FadeTime = fadeTime,
				Type = ExposedParameter.Volume
			};

			StartTweaking(parameter);

			return this;
		}

		IPlayerExclusive IPlayerExclusive.LowPassOthers(float freq ,float fadeTime)
        {
            if (!IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetToExclusiveMode();

            Parameter parameter = new Parameter()
            {
                Start = AudioConstant.MaxFrequence,
                Target = freq,
                FadeTime = fadeTime,
                Type = ExposedParameter.LowPass
            };

            StartTweaking(parameter);
            return this;
        }

        IPlayerExclusive IPlayerExclusive.HighPassOthers(float freq,float fadeTime)
        {
            if (!IsValidFrequence(freq))
            {
                return this;
            }

            Player.SetToExclusiveMode();

            Parameter parameter = new Parameter()
            {
                Start = AudioConstant.MinFrequence,
                Target = freq,
                FadeTime = fadeTime,
                Type = ExposedParameter.HighPass
            };

            StartTweaking(parameter);
            return this;
        }

        private bool IsValidFrequence(float freq)
        {
            if (freq < AudioConstant.MinFrequence || freq > AudioConstant.MaxFrequence)
            {
                LogWarning($"The given frequence should be in {AudioConstant.MinFrequence}Hz ~ {AudioConstant.MaxFrequence}Hz.");
                return false;
            }
            return true;
        }

        private void StartTweaking(Parameter parameter)
        {
            _mainTrackTweakingDict ??= new Dictionary<ExposedParameter, Coroutine>();

            if (_mainTrackTweakingDict.ContainsKey(parameter.Type))
            {
                Player.SafeStopCoroutine(_mainTrackTweakingDict[parameter.Type]);
            }
            else
            {
                _mainTrackTweakingDict.Add(parameter.Type, null);
            }
            _mainTrackTweakingDict[parameter.Type] = Player.StartCoroutine(TweakMainTrackParameter(parameter));
        }

        private IEnumerator TweakMainTrackParameter(Parameter para)
        {
            yield return SoundManager.Instance.SetMainGroupTrackParameter(para.Type, para.Target, para.FadeTime);
            if (IsPlaying)
            {
                yield return new WaitWhile(() => IsPlaying);
            }
            yield return SoundManager.Instance.SetMainGroupTrackParameter(para.Type, para.Start, para.FadeTime);
        }

        private void StopTweakingMainTrack(ExposedParameter paraType)
        {
            if(_mainTrackTweakingDict.ContainsKey(paraType))
			{
                _mainTrackTweakingDict.Remove(paraType);
			}
        }
    }

}