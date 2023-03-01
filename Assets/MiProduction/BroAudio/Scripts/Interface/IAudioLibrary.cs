namespace MiProduction.BroAudio
{
    public interface IAudioLibrary
    {
        public bool Validate(int index);

        public int ID { get; }
        public string EnumName { get; }

        public BroAudioClip Clip { get; }

    }

}