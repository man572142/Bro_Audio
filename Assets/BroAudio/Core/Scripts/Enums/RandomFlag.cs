namespace Ami.Extension
{
    [System.Flags]
    public enum RandomFlag
    {
        None = 0,
        Pitch = 1 << 0,
        Volume = 1 << 1,
    } 
}