using System;
using UnityEngine;

namespace Ami.Extension
{
    /// <summary>
    /// Displays a button that sets the value of the property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ValueButton : PropertyAttribute
    {
        public string Label;
        public object Value;
        public float ButtonWidth;

        public ValueButton(string label, object value, float buttonWidth = -1f)
        {
            Label = label;
            Value = value;
            ButtonWidth = buttonWidth;
        }
    }
}