using System;

namespace Ami.BroAudio.UI
{
    [Flags]
    public enum UiPointerEventType
    {
        None = 0,
        Click = 1 << 0, // 1
        Up = 1 << 1, // 2
        Down = 1 << 2, // 4
        Enter = 1 << 3, // 8
        Exit = 1 << 4, // 16
        BeginDrag = 1 << 5, // 32
        Drag = 1 << 6, // 64
        EndDrag = 1 << 7, // 128
        Drop = 1 << 8  // 256
    }
}