using System;
using UnityEngine;

namespace Ami.Extension
{
    /// <summary>
    /// Displays a line that indicates the relationship between the properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DerivativeProperty : PropertyAttribute
    {
        public bool IsEnd { get; private set; }

        public DerivativeProperty(bool isEnd = false)
        {
            IsEnd = isEnd;
        }
    } 
}