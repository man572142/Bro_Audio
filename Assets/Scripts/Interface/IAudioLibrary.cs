namespace MiProduction.BroAudio
{
    public interface IAudioLibrary
    {
        public bool Validate(int index);
        public string EnumName { get; }

    }

}