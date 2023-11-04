using UnityEngine;

namespace Ami.Extension
{
    public class BeautyEnum : PropertyAttribute
    {
        public readonly bool ShowSeparator;
        public readonly TextAnchor FieldTextAnchor;

        public BeautyEnum()
        {
            ShowSeparator = true;
            FieldTextAnchor = TextAnchor.MiddleCenter;
        }

        public BeautyEnum(bool showSeparator)
        {
            ShowSeparator = showSeparator;
            FieldTextAnchor = TextAnchor.MiddleLeft;
        }

        public BeautyEnum(TextAnchor fieldTextAnchor)
        {
            FieldTextAnchor = fieldTextAnchor;
        }

        public BeautyEnum(bool showSeparator, TextAnchor fieldTextAnchor)
        {
            ShowSeparator = showSeparator;
            FieldTextAnchor = fieldTextAnchor;
        }
    }
}