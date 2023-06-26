using System;
using System.Collections;
using System.Collections.Generic;
using MiProduction.Extension;
using UnityEngine;
using UnityEngine.Audio;
using static MiProduction.BroAudio.Utility;

namespace MiProduction.BroAudio.Runtime
{
	public class EffectAutomationHelper : IAutoResetWaitable
	{
		private class Tweaker
		{
			public Coroutine Coroutine;
			public bool IsTweaking;
			public float OriginValue;
		}

		public const string EffectTrackName = "Effect";
		public const string LowPassExposedName = EffectTrackName + "_LowPass";
		public const string HighPassExposedName = EffectTrackName + "_HighPass";

		private readonly MonoBehaviour _mono = null;
		private readonly AudioMixer _mixer = null;
		private readonly YieldInstructionWrapper _yieldWrapper = new YieldInstructionWrapper();
		private Dictionary<EffectType, Tweaker> _trackTweakingDict = null;

		public EffectAutomationHelper(MonoBehaviour mono, AudioMixer mixer)
		{
			_mono = mono;
			_mixer = mixer;
		}

		public WaitForSeconds ForSeconds(float seconds)
		{
			var forSeconds = new WaitForSeconds(seconds);
			_yieldWrapper.SetInstruction(forSeconds);
			return forSeconds;
		}

		public Coroutine Until(Coroutine coroutine)
		{
			_yieldWrapper.SetInstruction(coroutine);
			return coroutine;
		}

		public WaitUntil Until(Func<bool> predicate)
		{
			var wait = new WaitUntil(predicate);
			_yieldWrapper.SetInstruction(wait);
			return wait;
		}

		public IEnumerator Until(IEnumerator enumerator)
		{
			_yieldWrapper.SetInstruction(enumerator);
			return enumerator;
		}

		public WaitWhile While(Func<bool> predicate)
		{
			var wait = new WaitWhile(predicate);
			_yieldWrapper.SetInstruction(wait);
			return wait;
		}

		public void SetEffectTrackParameter(EffectParameter effect,Action onReset)
		{
			_trackTweakingDict ??= new Dictionary<EffectType, Tweaker>();

			if (effect.Type == EffectType.None)
			{
				ResetAllEffect(effect.FadeTime, effect.FadingEase,onReset);
				return;
			}

			float originValue = -1f;
			if (_trackTweakingDict.TryGetValue(effect.Type, out var tweaker))
			{
				originValue = tweaker.IsTweaking ? tweaker.OriginValue : -1f;
				_mono.SafeStopCoroutine(tweaker.Coroutine);
			}
			else
			{
				_trackTweakingDict.Add(effect.Type, null);
			}

			if (originValue == -1 && !TryGetCurrentEffectValue(effect.Type, out originValue))
			{
				return;
			}

			tweaker ??= new Tweaker();
			tweaker.OriginValue = originValue;
			tweaker.Coroutine = _mono.StartCoroutine(TweakTrackParameter(effect, originValue,onReset));
			tweaker.IsTweaking = true;
			_trackTweakingDict[effect.Type] = tweaker;
		}

		private IEnumerator TweakTrackParameter(EffectParameter effect, float originValue,Action onReset)
		{
			string paraName = GetEffectParameterName(effect.Type);

			yield return Tweak(originValue, effect.Value, effect.FadeTime, effect.FadingEase, paraName);

			if (_yieldWrapper.HasYieldInstruction())
			{
				yield return _yieldWrapper.Execute();
				yield return Tweak(effect.Value, originValue, effect.FadeTime, effect.FadingEase, paraName, onReset);
			}

			_trackTweakingDict[effect.Type].IsTweaking = false;
		}

		private IEnumerator Tweak(float from, float to, float fadeTime, Ease ease, string paraName, Action onTweakingFinshed = null)
		{
			var values = AnimationExtension.GetLerpValuesPerFrame(from, to, fadeTime, ease);

			foreach (float value in values)
			{
				_mixer.SetFloat(paraName, value);
				yield return null;
			}
			_mixer.SetFloat(paraName, to);
			onTweakingFinshed?.Invoke();
		}

		private bool TryGetCurrentEffectValue(EffectType effectType, out float value)
		{
			string paraName = GetEffectParameterName(effectType);
			if (!_mixer.GetFloat(paraName, out value))
			{
				LogError($"Can't get exposed parameter[{paraName}] Please re-import BroAudioMixer");
				return false;
			}
			return true;
		}

		private void ResetAllEffect(float fadeTime, Ease ease,Action onReset)
		{
			foreach (var pair in _trackTweakingDict)
			{
				Tweaker tweaker = pair.Value;
				EffectType effectType = pair.Key;
				if (TryGetCurrentEffectValue(effectType, out float current))
				{
					string paraName = GetEffectParameterName(effectType);
					_mono.SafeStopCoroutine(tweaker.Coroutine);
					tweaker.Coroutine = _mono.StartCoroutine(Tweak(current, tweaker.OriginValue, fadeTime, ease, paraName, onReset));
					tweaker.IsTweaking = true;
					tweaker.OriginValue = GetEffectDefaultValue(effectType);
				}
			}
		}

		private string GetEffectParameterName(EffectType effectType)
		{
			return effectType switch
			{
				EffectType.Volume => EffectTrackName,
				EffectType.LowPass => LowPassExposedName,
				EffectType.HighPass => HighPassExposedName,
				_ => string.Empty,
			};
		}

		private float GetEffectDefaultValue(EffectType effectType)
		{
			return effectType switch
			{
				EffectType.Volume => AudioConstant.FullDecibelVolume,
				EffectType.LowPass => AudioConstant.MaxFrequence,
				EffectType.HighPass => AudioConstant.MinFrequence,
				_ => -1f,
			};
		}
	} 
}
