using System;

[Flags]
public enum AudioType
{
    None = 0,

    Music = 1,
    UI = 2,
    Ambience = 4,
    SFX = 8,
    VoiceOver = 16,

    All = Music | UI | Ambience | SFX | VoiceOver,
}

//public static class ConstantID
//{
//    public const int Music = 1;
//    public const int UI = 100;
//    public const int Ambience = 10000;
//    public const int Sfx = 1000000;
//    public const int VoiceOver = 100000000;
//    public const int Limit = int.MaxValue;

//    public static int[] AudioIDs = { Music, UI, Sfx, VoiceOver, Limit };
//}