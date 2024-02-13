using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
    public static class EaseExtension
    {
        public static float SetEase(this float value, Ease ease)
        {
            Mathf.Clamp01(value);

            switch (ease)
            {
                case Ease.Linear:
                    return value;
                case Ease.InQuad:
                    return Mathf.Pow(value, 2);
                case Ease.InCubic:
                    return Mathf.Pow(value, 3);
                case Ease.InQuart:
                    return Mathf.Pow(value, 4);
                case Ease.InQuint:
                    return Mathf.Pow(value, 5);
                case Ease.InSine:
                    return 1 - Mathf.Cos((value * Mathf.PI) / 2);
                case Ease.InCirc:
                    return 1 - Mathf.Sqrt(1 - Mathf.Pow(value, 2));
                case Ease.OutQuad:
                    return 1 - Mathf.Pow((1 - value), 2);
                case Ease.OutCubic:
                    return 1 - Mathf.Pow((1 - value), 3);
                case Ease.OutQuart:
                    return 1 - Mathf.Pow((1 - value), 4);
                case Ease.OutQuint:
                    return 1 - Mathf.Pow((1 - value), 5);
                case Ease.OutSine:
                    return Mathf.Sin((value * Mathf.PI) / 2);
                case Ease.OutCirc:
                    return Mathf.Sqrt(1 - Mathf.Pow(value - 1, 2));
                case Ease.InOutQuad:
                    return value < 0.5f ? 2 * Mathf.Pow(value, 2) : 1 - Mathf.Pow(-2 * value + 2, 2) / 2;
                case Ease.InOutCubic:
                    return value < 0.5f ? 2 * Mathf.Pow(value, 3) : 1 - Mathf.Pow(-2 * value + 2, 3) / 2;
                case Ease.InOutQuart:
                    return value < 0.5f ? 2 * Mathf.Pow(value, 4) : 1 - Mathf.Pow(-2 * value + 2, 4) / 2;
                case Ease.InOutQuint:
                    return value < 0.5f ? 2 * Mathf.Pow(value, 5) : 1 - Mathf.Pow(-2 * value + 2, 5) / 2;
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * value) - 1) / 2;
                case Ease.InOutCirc:
                    return value < 0.5 ?
                        (1 - Mathf.Sqrt(1 - Mathf.Pow(2 * value, 2))) / 2 : (Mathf.Sqrt(1 - Mathf.Pow(-2 * value + 2, 2)) + 1) / 2;
                default:
                    return 0;
            }
        }
    } 

    public enum Ease
    {
	    Linear,

	    InQuad,
	    InCubic,
	    InQuart,
	    InQuint,
	    InSine,
	    InCirc,

	    OutQuad,
	    OutCubic,
	    OutQuart,
	    OutQuint,
	    OutSine,
	    OutCirc,

	    InOutQuad,
	    InOutCubic,
	    InOutQuart,
	    InOutQuint,
	    InOutSine,
	    InOutCirc,
    }
}