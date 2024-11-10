using System;
using UnityEngine;

namespace Ami.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Button : PropertyAttribute
    {
        public string Label;
        public object Value;
        public float ButtonWidth;

        public Button(string label, object value, float buttonWidth = -1f)
        {
            Label = label;
            Value = value;
            ButtonWidth = buttonWidth;
        }
    }
}