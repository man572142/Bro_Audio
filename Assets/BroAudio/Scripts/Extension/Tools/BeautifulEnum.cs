using UnityEngine;

namespace Ami.Extension
{
    public class BeautifulEnum : PropertyAttribute
    {
        public readonly bool ShowSeparator;
        public readonly TextAnchor FieldTextAnchor;

        public BeautifulEnum()
        {
            ShowSeparator = true;
            FieldTextAnchor = TextAnchor.MiddleCenter;
        }

        public BeautifulEnum(bool showSeparator)
        {
            ShowSeparator = showSeparator;
            FieldTextAnchor = TextAnchor.MiddleLeft;
        }

        public BeautifulEnum(TextAnchor fieldTextAnchor)
        {
            FieldTextAnchor = fieldTextAnchor;
        }

        public BeautifulEnum(bool showSeparator, TextAnchor fieldTextAnchor)
        {
            ShowSeparator = showSeparator;
            FieldTextAnchor = fieldTextAnchor;
        }
    }
}