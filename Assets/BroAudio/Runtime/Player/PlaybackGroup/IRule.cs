namespace Ami.BroAudio
{
    public interface IRule
    {
        PlaybackGroup.IsPlayableDelegate RuleMethod { get; }
    }
}