namespace Ami.BroAudio.Data
{
    public interface IEntityIdentity
    {
        bool Validate();
        int ID { get; }
        string Name { get; }
    }
}