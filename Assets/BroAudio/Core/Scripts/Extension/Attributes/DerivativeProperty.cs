using UnityEngine;

namespace Ami.Extension
{
    public class DerivativeProperty : PropertyAttribute
    {
        public bool IsEnd { get; private set; }

        public DerivativeProperty(bool isEnd = false)
        {
            IsEnd = isEnd;
        }
    } 
}