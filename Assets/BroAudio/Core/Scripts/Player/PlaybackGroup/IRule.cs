namespace Ami.BroAudio
{
    public interface IRule
    {
        PlaybackGroup.PlayableDelegate RuleDelegate { get; }
    }
}