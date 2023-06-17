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
		private class Tweaker
		{
			public Coroutine Coroutine;
			public bool IsTweaking;
			public float OriginValue;
		}

		public const string MainGroupName = "Main";
		public const string LowPassExposedName = MainGroupName + "_LowPass";
		public const string HighPassExposedName = MainGroupName + "_HighPass";
		public const string ExclusiveTrackName = "Exclusive";

		private Dictionary<EffectType, Tweaker>  _mainTrackTweakingDict = null;

		private void InitAuxTrack()
		{
		}

		public void SetMainGroupTrackParameter(EffectParameter effect, Func<bool> resetCondition, Ease ease = Ease.Linear)
		{
			_mainTrackTweakingDict ??= new Dictionary<EffectType, Tweaker>();

			float originValue = -1f;
			if(_mainTrackTweakingDict.TryGetValue(effect.Type,out var tweaker))
			{
				originValue = tweaker.IsTweaking ? tweaker.OriginValue : -1f;
				this.SafeStopCoroutine(tweaker.Coroutine);
			}
			else
			{
				_mainTrackTweakingDict.Add(effect.Type, null);
			}

			if (originValue == -1 && !TryGetMainGroupFloatValue(effect.Type, out originValue))
			{
				return;
			}

			tweaker ??= new Tweaker();
			tweaker.OriginValue = originValue;
			tweaker.Coroutine = StartCoroutine(TweakMainTrackParameter(effect,originValue ,resetCondition, ease));
			tweaker.IsTweaking = true;
			_mainTrackTweakingDict[effect.Type] = tweaker;
		}

		private IEnumerator TweakMainTrackParameter(EffectParameter effect,float originValue,Func<bool> resetCondition, Ease ease = Ease.Linear)
		{
			string paraName = GetMainEffectParameterName(effect.Type);

			yield return SetTrackParameter(originValue, effect.Value, effect.FadeTime, ease, paraName);
			if (resetCondition != null && !resetCondition.Invoke())
			{
				yield return new WaitUntil(resetCondition);
				yield return SetTrackParameter(effect.Value, originValue, effect.FadeTime, ease, paraName);
			}

			_mainTrackTweakingDict[effect.Type].IsTweaking = false;
		}

		private IEnumerator SetTrackParameter(float from, float to, float fadeTime, Ease ease, string paraName)
		{
			var values = AnimationExtension.GetLerpValuesPerFrame(from, to, fadeTime, ease);

			foreach (float value in values)
			{
				_broAudioMixer.SetFloat(paraName, value);
				yield return null;
			}
			_broAudioMixer.SetFloat(paraName, to);
		}

		private string GetMainEffectParameterName(EffectType effectType)
		{
			return effectType switch
			{
				EffectType.Volume => MainGroupName,
				EffectType.LowPass => LowPassExposedName,
				EffectType.HighPass => HighPassExposedName,
				_ => string.Empty,
			};
		}

		private bool TryGetMainGroupFloatValue(EffectType effectType, out float value)
		{
			string paraName = GetMainEffectParameterName(effectType);
			if (!_broAudioMixer.GetFloat(paraName, out value))
			{
				LogError($"Can't get exposed parameter:{paraName}. Please re-import BroAudioMixer");
				return false;
			}
			return true;

		}
	}
}
