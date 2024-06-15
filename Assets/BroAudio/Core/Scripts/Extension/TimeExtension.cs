using UnityEngine;

namespace Ami.Extension
{
    public static class TimeExtension
    {
        public const int SecondInMilliseconds = 1000;

#if UNITY_2020_2_OR_NEWER
        public static int RealTimeSinceStartupAsMilliseconds => SecToMs(Time.realtimeSinceStartupAsDouble);
        public static int UnscaledCurrentFrameBeganTime => SecToMs(Time.unscaledTimeAsDouble); 
#else
        public static int RealTimeSinceStartupAsMilliseconds => SecToMs(Time.realtimeSinceStartup);
        public static int UnscaledCurrentFrameBeganTime => SecToMs(Time.unscaledTime);
#endif

        public static int SecToMs(double seconds)
        {
            return (int)(seconds * SecondInMilliseconds);
        }
    } 
}