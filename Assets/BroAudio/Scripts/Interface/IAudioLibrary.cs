namespace Ami.BroAudio.Data
{
    public interface IAudioLibrary
    {
        public bool Validate();
        public int ID { get; }
        public string Name { get; }
    }
}