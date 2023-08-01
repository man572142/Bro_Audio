using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiProduction.Extension
{
	public static class AnimationExtension
	{
		public static IEnumerable<float> GetLerpValuesPerFrame(float start, float target, float duration, Ease ease)
		{
			float currentTime = 0f;
			float currentValue = start;
			float newValue;
			while (currentTime < duration)
			{
				currentTime += Time.deltaTime;
				newValue = Mathf.Lerp(currentValue, target, (currentTime / duration).SetEase(ease));
				yield return newValue;
			}
		}
	} 
}
