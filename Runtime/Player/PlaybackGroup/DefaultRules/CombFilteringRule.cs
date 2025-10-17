namespace Ami.BroAudio
{
    [System.Serializable]
    public class CombFilteringRule : Rule<float>
    {
        public CombFilteringRule(float value) : base(value)
        {
        }

        public static implicit operator CombFilteringRule(float value) => new CombFilteringRule(value);
    }
}
