namespace Ami.BroAudio.Data
{
    public interface IAudioLibrary
    {
        bool Validate();
        int ID { get; }
        string Name { get; }
    }
}