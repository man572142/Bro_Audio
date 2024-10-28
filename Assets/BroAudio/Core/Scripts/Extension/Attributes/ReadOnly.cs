using UnityEngine;
using System;

namespace Ami.Extension
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnly : PropertyAttribute
    {
    }
}