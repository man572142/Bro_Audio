namespace MiProduction.BroAudio.Data
{
    public interface IAudioEntity
    {
        public bool Validate(int index);
        public int ID { get; }
        public string Name { get; }
    }
}