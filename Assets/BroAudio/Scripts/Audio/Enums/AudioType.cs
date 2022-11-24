using System;

[Flags]
public enum AudioType
{
    None = 0,

    SFX = 1,
    Music = 2,
    Ambience = 4,
    UI = 8,

    StandOut = 16,

    All = SFX | Music | Ambience | UI,

}
