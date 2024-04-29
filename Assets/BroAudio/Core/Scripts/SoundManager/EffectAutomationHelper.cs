using System;
using System.Collections;
using System.Collections.Generic;
using Ami.Extension;
using UnityEngine;
using UnityEngine.Audio;
using Ami.BroAudio.Tools;

namespace Ami.BroAudio.Runtime
{
	public class EffectAutomationHelper : CoroutineBehaviour,IAutoResetWaitable
    {
		private class Tweaker
		{
			public bool IsTweaking;
			public Coroutine Coroutine;
			public List<ITweakingWaitable> WaitableList;
			public float OriginValue;
		}

		public interface ITweakingWaitable
		{
			Effect Effect { get; }
			bool IsFinished();
			IEnumerator GetYieldInstruction();
		}

		private class TweakingWaitableBase : ITweakingWaitable
		{
			public Effect Effect { get; set; }
			public TweakingWaitableBase(Effect effect) => Effect = effect;
			public IEnumerator GetYieldInstruction() => null;
			public bool IsFinished() => false;
		}

		private abstract class TweakingWaitableDecorator : ITweakingWaitable
		{
			protected ITweakingWaitable Base;
			public void AttachTo(ITweakingWaitable waitable) => Base = waitable;

			public Effect Effect => Base.Effect; 
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
                _waitUntil = _waitUntil ?? new WaitUntil(IsFinished);
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
			// this should be called after the first tweak is started, the purpose of decorating is to know when will it stop.
			if (_latestEffect == EffectType.None)
			{
				Debug.LogWarning(Utility.LogTitle + $"AutoResetWaitable on {_latestEffect} is not supported.");
			}
			else if (_tweakerDict.TryGetValue(_latestEffect, out var tweaker))
			{
				int lastIndex = tweaker.WaitableList.Count - 1;
				var current = tweaker.WaitableList[lastIndex];
				if(current is TweakingWaitableBase)
				{
					decoration.AttachTo(current);
					tweaker.WaitableList[lastIndex] = decoration;
				}
				else
				{
					Debug.LogError(Utility.LogTitle + $"The latest waitable isn't the base type:{nameof(TweakingWaitableBase)}");
				}
			}
		}

		public void SetEffectTrackParameter(Effect effect, Action<EffectType> OnReset)
		{
			_latestEffect = effect.Type;
			if (_latestEffect == EffectType.None)
			{
				ResetAllEffect(effect, OnReset);
				return;
			}

			if (!_tweakerDict.TryGetValue(effect.Type, out var tweaker))
			{
				tweaker = new Tweaker();
				tweaker.OriginValue = GetEffectDefaultValue(effect.Type);
				_tweakerDict.Add(effect.Type, tweaker);
			}

			tweaker.WaitableList = tweaker.WaitableList ?? new List<ITweakingWaitable>();
			tweaker.WaitableList.Add(new TweakingWaitableBase(effect));

			if(!tweaker.IsTweaking)
			{
				StartCoroutineAndReassign(TweakTrackParameter(tweaker, OnTweakingFinished), ref tweaker.Coroutine);
				if(effect.IsDominator)
				{
					SwitchMainTrackMode(true);
                }	
			}
			
			void OnTweakingFinished()
			{
				if(tweaker.OriginValue == GetEffectDefaultValue(effect.Type))
				{
					OnReset?.Invoke(effect.Type);
					if(effect.IsDominator)
					{
						SwitchMainTrackMode(false);
                    }
				}
			}
		}

		private void SwitchMainTrackMode(bool isDominatorActive)
		{
			string from = isDominatorActive ? BroName.MainTrackName : BroName.MainDominatedTrackName;
			string to = isDominatorActive ? BroName.MainDominatedTrackName : BroName.MainTrackName;

			_mixer.ChangeChannel(from, to, AudioConstant.FullDecibelVolume);
        }

		private IEnumerator TweakTrackParameter(Tweaker tweaker,Action onFinished)
		{
			tweaker.IsTweaking = true;
			while (tweaker.WaitableList.Count > 0)
			{
				int lastIndex = tweaker.WaitableList.Count - 1;
				var effect = tweaker.WaitableList[lastIndex].Effect;
				string paraName = GetEffectParameterName(effect, out bool hasSecondaryParameter);
				float currentValue = GetCurrentValue(effect);

				yield return Tweak(currentValue, effect.Value, effect.Fading.FadeIn, effect.Fading.FadeInEase, paraName, hasSecondaryParameter);
				// waitable should be decorated after this tweak

				var waitable = tweaker.WaitableList[lastIndex];
				IEnumerator yieldInstruction = waitable.GetYieldInstruction();
				if (yieldInstruction == null)
				{
					// save value if it's a normal SetEffect 
					tweaker.OriginValue = effect.Value;
					tweaker.WaitableList.Clear();
					break;
				}
				else if (!waitable.IsFinished())
				{
					yield return yieldInstruction;
				}

				if(tweaker.WaitableList.Count == 1)
				{
					// auto reset to origin after last waitable 
					yield return Tweak(GetCurrentValue(effect), tweaker.OriginValue, effect.Fading.FadeOut, effect.Fading.FadeOutEase, paraName, hasSecondaryParameter);
				}

				tweaker.WaitableList.RemoveAt(lastIndex);
			}
			tweaker.IsTweaking = false;
			onFinished?.Invoke();
		}

		private IEnumerator Tweak(float from, float to, float fadeTime, Ease ease, string paraName,bool hasSecondaryParameter = false, Action onTweakingFinshed = null)
		{
			if(from == to)
			{
				yield break;
			}
			var values = AnimationExtension.GetLerpValuesPerFrame(from, to, fadeTime, ease);
			string secondaryParaName = hasSecondaryParameter ? paraName + "2" : null;

            foreach (float value in values)
			{
				_mixer.SafeSetFloat(paraName, value);
				_mixer.SafeSetFloat(secondaryParaName, value);
				yield return null;
            }
			_mixer.SafeSetFloat(paraName, to);
            _mixer.SafeSetFloat(secondaryParaName, to);
            onTweakingFinshed?.Invoke();
		}

		private bool TryGetCurrentValue(Effect effect, out float value)
		{
			string paraName = GetEffectParameterName(effect, out bool _);
			if (!_mixer.SafeGetFloat(paraName, out value))
			{
				Debug.LogError(Utility.LogTitle + $"Can't get exposed parameter of {effect.Type}");
				return false;
			}
			return true;
		}

		private float GetCurrentValue(Effect effect)
		{
			if(TryGetCurrentValue(effect , out var result))
			{
				return result;
			}
			return default;
		}

		private void ResetAllEffect(Effect effect,Action<EffectType> OnResetFinished)
		{
			int tweakingCount = 0;
			foreach (var pair in _tweakerDict)
			{
				Tweaker tweaker = pair.Value;
				EffectType effectType = pair.Key;
				if (TryGetCurrentValue(effect, out float current))
				{
					string paraName = GetEffectParameterName(effect, out bool hasSecondaryParameter);
					SafeStopCoroutine(tweaker.Coroutine);
                    tweaker.Coroutine = StartCoroutine(Tweak(current, tweaker.OriginValue, effect.Fading.FadeOut, effect.Fading.FadeOutEase, paraName, hasSecondaryParameter, OnTweakingFinished));
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

		private string GetEffectParameterName(Effect effect, out bool hasSecondaryParameter)
		{
			hasSecondaryParameter = false;
            switch (effect.Type)
			{
				case EffectType.Volume:
					if(effect.IsDominator)
					{
						return BroName.MainTrackName;
                    }
					else
					{
						Debug.LogError(Utility.LogTitle + $"{effect.Type} is only supported on Dominator");
						return string.Empty;
					}
				case EffectType.LowPass:
					hasSecondaryParameter = SoundManager.Instance.Setting.AudioFilterSlope == FilterSlope.FourPole;
                    return effect.IsDominator ? BroName.Dominator_LowPassParaName : BroName.LowPassParaName;
				case EffectType.HighPass:
                    hasSecondaryParameter = SoundManager.Instance.Setting.AudioFilterSlope == FilterSlope.FourPole;
                    return effect.IsDominator ? BroName.Dominator_HighPassParaName : BroName.HighPassParaName;
				case EffectType.Custom:
					return effect.CustomExposedParameter;
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
					return AudioConstant.MaxFrequency;
				case EffectType.HighPass:
					return AudioConstant.MinFrequency;
				default:
					return -1f;
			}
		}
	}
}
