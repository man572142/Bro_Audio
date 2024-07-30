namespace Ami.Extension
{
	public static class FlagsExtension
	{
        public enum FlagsRangeType
        {
            Included,
            Excluded,
        }

        public static int GetFlagsOnCount(int flags)
        {
            int count = 0;
            while (flags != 0)
            {
                flags = flags & (flags - 1);
                count++;
                if(count > 32) // integer has only 32-bit max
                {
                    UnityEngine.Debug.LogError("count flags is failed");
                    break;
				}
            }
            return count;
        }

        public static int GetFlagsRange(int minIndex,int maxIndex, FlagsRangeType rangeType)
        {
            int flagsRange = 0;
            for(int i = minIndex; i <= maxIndex;i++)
            {
                flagsRange += 1 << i;
            }

            return rangeType switch 
            {
                FlagsRangeType.Included => flagsRange,
                FlagsRangeType.Excluded => ~flagsRange,
                _ => default,
            };
        }
    }
}