namespace Ami.BroAudio.Editor
{
    [System.Flags]
    public enum VolumeSliderOptions
    {
        Slider = 0,
        Label = 1 << 0,
        Field = 1 << 1,
        VUMeter = 1 << 2,
        SnapFullVolume = 1 << 3,
    }
}