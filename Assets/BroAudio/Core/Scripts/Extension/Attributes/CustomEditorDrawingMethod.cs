using System;
using System.Reflection;
using UnityEngine;

namespace Ami.Extension
{
    public class CustomEditorDrawingMethod : PropertyAttribute
    {
        public MethodInfo Method;

        public CustomEditorDrawingMethod(Type type, string method)
        {
            Method = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        }
    } 
}
