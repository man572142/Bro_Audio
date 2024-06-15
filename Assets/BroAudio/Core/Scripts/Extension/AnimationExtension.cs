using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
	public static class AnimationExtension
	{
		public static IEnumerable<float> GetLerpValuesPerFrame(float start, float target, float duration, Ease ease)
		{
			float currentTime = 0f;
			while (currentTime < duration)
			{
				currentTime += Time.deltaTime;
				yield return Mathf.Lerp(start, target, (currentTime / duration).SetEase(ease));
            }
		}
	} 
}
