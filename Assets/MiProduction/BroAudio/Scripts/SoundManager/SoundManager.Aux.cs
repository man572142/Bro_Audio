using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MiProduction.BroAudio.Utility;
using static MiProduction.Extension.CoroutineExtension;
using MiProduction.Extension;
using System;

namespace MiProduction.BroAudio.Runtime
{
	public partial class SoundManager : MonoBehaviour
	{
		public event Action<ExposedParameter> OnStopMainTrackTweaking;

		public const string MainGroupName = "Main";
		public const string LowPassExposedName = MainGroupName + "_LowPass";
		public const string HighPassExposedName = MainGroupName + "_HighPass";
		public const string ExclusiveTrackName = "Exclusive";

		private Dictionary<ExposedParameter, Coroutine>  _mainTrackTweakingDict = null;

		private void InitAuxTrack()
		{
		}

		public Coroutine SetMainGroupTrackParameter(ExposedParameter paraType,float targetValue, float fadeTime,Ease ease = Ease.Linear)
		{
			string paraName = paraType switch
			{
				ExposedParameter.Volume => MainGroupName,
				ExposedParameter.LowPass => LowPassExposedName,
				ExposedParameter.HighPass => HighPassExposedName,
				_ => string.Empty,
			};

			_mainTrackTweakingDict ??= new Dictionary<ExposedParameter, Coroutine>();

			if(_mainTrackTweakingDict.ContainsKey(paraType))
			{
				this.SafeStopCoroutine(_mainTrackTweakingDict[paraType]);
				OnStopMainTrackTweaking?.Invoke(paraType);
			}
			else
			{
				_mainTrackTweakingDict.Add(paraType, null);
			}
			_mainTrackTweakingDict[paraType] = StartCoroutine(SetTrackParameter(targetValue, fadeTime, ease, paraName));

			return _mainTrackTweakingDict[paraType];
		}

		private IEnumerator SetTrackParameter(float target, float fadeTime, Ease ease, string parameterName)
		{
			if(!_broAudioMixer.GetFloat(parameterName, out float start))
			{
				LogError($"Can't get exposed parameter:{parameterName}. Please re-import BroAudioMixer");
				yield break;
			}

			var values = AnimationExtension.GetLerpValuesPerFrame(start,target,fadeTime,ease);

			foreach(float value in values)
			{
				_broAudioMixer.SetFloat(parameterName, value);
				yield return null;
			}
			_broAudioMixer.SetFloat(parameterName, target);
		}
	}
}
