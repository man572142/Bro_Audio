using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ami.Extension
{
    public static class EventExtension
    {
        public static bool IsDoubleClicking(Rect rect, Event current = null)
        {
            current ??= Event.current;
            return current.type == EventType.MouseDown && current.button == 0 && current.clickCount == 2 && rect.Contains(current.mousePosition);
        }

        public static bool IsRightClick(Rect rect, Event current = null)
        {
            current ??= Event.current;
            return current.type == EventType.MouseDown && current.button == 1 && rect.Contains(current.mousePosition);
        }
    } 
}
