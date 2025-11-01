namespace Ami.BroAudio
{
    [System.Serializable]
    public class MaxPlayableCountRule : Rule<int>
    {
        public MaxPlayableCountRule(int value) : base(value)
        {
        }

        public static implicit operator MaxPlayableCountRule(int value) => new MaxPlayableCountRule(value);
    }
}