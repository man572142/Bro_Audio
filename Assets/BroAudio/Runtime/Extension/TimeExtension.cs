using UnityEngine;

namespace Ami.Extension
{
    public static class TimeExtension
    {
        public const int SecondInMilliseconds = 1000;

        public static int RealTimeSinceStartupAsMilliseconds => SecToMs(Time.realtimeSinceStartupAsDouble);
        public static int UnscaledCurrentFrameBeganTime => SecToMs(Time.unscaledTimeAsDouble); 

        public static int SecToMs(double seconds)
        {
            return (int)(seconds * SecondInMilliseconds);
        }
    } 
}