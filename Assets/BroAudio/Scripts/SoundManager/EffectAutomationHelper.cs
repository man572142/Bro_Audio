using System;
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Tools;
using static Ami.BroAudio.Tools.BroLog;

namespace Ami.BroAudio.Runtime
{
	public class EffectAutomationHelper : CoroutineBehaviour,IAutoResetWaitable
    {
		private class Tweaker
		{
			public Coroutine Coroutine;
			public List<ITweakingWaitable> WaitableList;
			public float OriginValue;

			public bool TryGetLastWaitable(out ITweakingWaitable waitable)
			{
				if (WaitableList != null && WaitableList.Count > 0)
				{
					waitable = WaitableList[WaitableList.Count - 1];
					return true;
				}
				waitable = default;
				return false;
			}

			public void RemoveLastWaitable()
			{
				if (WaitableList != null && WaitableList.Count > 0)
				{
					WaitableList.RemoveAt(WaitableList.Count - 1);
				}
			}
		}

		public interface ITweakingWaitable
		{
			EffectParameter Effect { get; }
			bool IsFinished();
			IEnumerator GetYieldInstruction();
		}

		private class TweakingWaitable : ITweakingWaitable
		{
			public EffectParameter Effect { get; set; }
			public TweakingWaitable(EffectParameter effect) => Effect = effect;
			public IEnumerator GetYieldInstruction() => null;
			public bool IsFinished() => false;
		}

		private abstract class TweakingWaitableDecorator : ITweakingWaitable
		{
			protected ITweakingWaitable Base;
			public void AttachTo(ITweakingWaitable waitable) => Base = waitable;

			public EffectParameter Effect => Base.Effect; 
			public abstract IEnumerator GetYieldInstruction();
			public abstract bool IsFinished();
		}

		private class TweakAndWaitSeconds : TweakingWaitableDecorator
		{
			public readonly float EndTime;
			private WaitUntil _waitUntil = null;

			public TweakAndWaitSeconds(float seconds)
			{
				EndTime = Time.time + seconds;
			}

			public override bool IsFinished() => Time.time >= EndTime;

			public override IEnumerator GetYieldInstruction()
			{
				if(_waitUntil == null)
					_waitUntil = new WaitUntil(IsFinished);
				yield return _waitUntil;
			}
		}

		private class TweakAndWaitUntil : TweakingWaitableDecorator
		{
			public readonly IEnumerator Enumerator;
			public readonly Func<bool> Condition;

			public TweakAndWaitUntil(IEnumerator enumerator, Func<bool> condition)
			{
				Enumerator = enumerator;
				Condition = condition;
			}

			public override IEnumerator GetYieldInstruction() => Enumerator;
			public override bool IsFinished() => Condition();
		}


		public const string EffectTrackName = "Effect";
		public const string LowPassExposedName = EffectTrackName + "_LowPass";
		public const string HighPassExposedName = EffectTrackName + "_HighPass";

		private readonly AudioMixer _mixer = null;
		private Dictionary<EffectType,Tweaker> _tweakerDict = new Dictionary<EffectType, Tweaker>();
		private EffectType _latestEffect = default;


		public EffectAutomationHelper(MonoBehaviour mono, AudioMixer mixer) : base(mono)
		{
			_mixer = mixer;
		}

		public WaitForSeconds ForSeconds(float seconds)
		{
			var decoration = new TweakAndWaitSeconds(seconds);
			DecorateTweakingWaitable(decoration);
			return new WaitForSeconds(seconds);
		}

		public WaitUntil Until(Func<bool> condition)
		{
			var instruction = new WaitUntil(condition);
			var decoration = new TweakAndWaitUntil(instruction,condition);
			DecorateTweakingWaitable(decoration);
			return instruction;
		}

		public WaitWhile While(Func<bool> condition)
		{
			var instruction = new WaitWhile(condition);
			var decoration = new TweakAndWaitUntil(instruction, InvertCondition);
			DecorateTweakingWaitable(decoration);
			return instruction;

			bool InvertCondition() => !condition();
		}


		private void DecorateTweakingWaitable(TweakingWaitableDecorator decoration)
		{
			EffectType effectType = _latestEffect;
			if (effectType == EffectType.None)
			{
				LogWarning("AutoResetWaitable on EffectType.None is currently not supported.");
			}
			else if (_tweakerDict.TryGetValue(effectType,out var tweaker))
			{
				int lastIndex = tweaker.WaitableList.Count - 1;
				var current = tweaker.WaitableList[lastIndex];
				if(current is TweakingWaitable)
				{
					decoration.AttachTo(current);
					tweaker.WaitableList[lastIndex] = decoration;
				}
				else
				{
					LogError($"The latest waitable isn't the base type:{nameof(TweakingWaitable)}");
				}
			}
		}

		public void SetEffectTrackParameter(EffectParameter effect, Action<EffectType> OnReset)
		{
			_latestEffect = effect.Type;

			if (effect.Type == EffectType.None)
			{
				ResetAllEffect(effect.FadeTime, effect.FadingEase, OnReset);
				return;
			}

			if (!_tweakerDict.TryGetValue(effect.Type, out var tweaker))
			{
				tweaker = new Tweaker();
				tweaker.OriginValue = GetEffectDefaultValue(effect.Type);
				_tweakerDict.Add(effect.Type, tweaker);
			}

			if(tweaker.WaitableList == null)
				tweaker.WaitableList = new List<ITweakingWaitable>();
			tweaker.WaitableList.Add(new TweakingWaitable(effect));

			StartCoroutineAndReassign(TweakTrackParameter(tweaker, OnTweakingFinished), ref tweaker.Coroutine);

			void OnTweakingFinished()
			{
				if(tweaker.OriginValue == GetEffectDefaultValue(effect.Type))
				{
					OnReset?.Invoke(effect.Type);
				}
			}
		}

		private IEnumerator TweakTrackParameter(Tweaker tweaker,Action onFinished)
		{
			while (tweaker.WaitableList.Count > 0)
			{
				int lastIndex = tweaker.WaitableList.Count - 1;
				var effect = tweaker.WaitableList[lastIndex].Effect;

				string paraName = GetEffectParameterName(effect.Type);
				float currentValue = GetCurrentValue(effect.Type);

				yield return Tweak(currentValue, effect.Value, effect.FadeTime, effect.FadingEase, paraName);

				var waitable = tweaker.WaitableList[lastIndex];
				if (waitable.GetYieldInstruction() == null)
				{
					tweaker.OriginValue = effect.Value;
					tweaker.WaitableList.Clear();
					break;
				}
				else if (!waitable.IsFinished())
				{
					yield return waitable.GetYieldInstruction();
				}

				if(tweaker.WaitableList.Count == 1)
				{
					// auto reset to origin after last waitable
					yield return Tweak(GetCurrentValue(effect.Type), tweaker.OriginValue, effect.FadeTime, effect.FadingEase, paraName);
				}

				tweaker.WaitableList.RemoveAt(lastIndex);
			}

			onFinished?.Invoke();
		}

		private IEnumerator Tweak(float from, float to, float fadeTime, Ease ease, string paraName, Action onTweakingFinshed = null)
		{
			if(from == to)
			{
				yield break;
			}
			var values = AnimationExtension.GetLerpValuesPerFrame(from, to, fadeTime, ease);
			foreach (float value in values)
			{
				_mixer.SetFloat(paraName, value);
				yield return null;
			}
			_mixer.SetFloat(paraName, to);
			onTweakingFinshed?.Invoke();
		}

		private bool TryGetCurrentValue(EffectType effectType, out float value)
		{
			string paraName = GetEffectParameterName(effectType);
			if (!_mixer.GetFloat(paraName, out value))
			{
				LogError($"Can't get exposed parameter[{paraName}] Please re-import {BroName.MixerName}");
				return false;
			}
			return true;
		}

		private float GetCurrentValue(EffectType effectType)
		{
			if(TryGetCurrentValue(effectType , out var result))
			{
				return result;
			}
			return default;
		}

		private void ResetAllEffect(float fadeTime, Ease ease,Action<EffectType> OnResetFinished)
		{
			int tweakingCount = 0;
			foreach (var pair in _tweakerDict)
			{
				Tweaker tweaker = pair.Value;
				EffectType effectType = pair.Key;
				if (TryGetCurrentValue(effectType, out float current))
				{
					string paraName = GetEffectParameterName(effectType);
					SafeStopCoroutine(tweaker.Coroutine);
					tweaker.Coroutine = StartCoroutine(Tweak(current, tweaker.OriginValue, fadeTime, ease, paraName,OnTweakingFinished));
					tweaker.OriginValue = GetEffectDefaultValue(effectType);
					tweaker.WaitableList.Clear();
					tweakingCount++;
				}
			}

			void OnTweakingFinished()
			{
				tweakingCount--;
				if(tweakingCount <= 0)
				{
					OnResetFinished?.Invoke(EffectType.All);
				}
			}
		}

		private string GetEffectParameterName(EffectType effectType)
		{
			switch (effectType)
			{
				case EffectType.Volume:
					return EffectTrackName;
				case EffectType.LowPass:
					return LowPassExposedName;
				case EffectType.HighPass:
					return HighPassExposedName;
				default:
					return string.Empty;
			}
		}

		private float GetEffectDefaultValue(EffectType effectType)
		{
			switch (effectType)
			{
				case EffectType.Volume:
					return AudioConstant.FullDecibelVolume;
				case EffectType.LowPass:
					return AudioConstant.MaxFrequence;
				case EffectType.HighPass:
					return AudioConstant.MinFrequence;
				default:
					return -1f;
			}
		}
	}
}
