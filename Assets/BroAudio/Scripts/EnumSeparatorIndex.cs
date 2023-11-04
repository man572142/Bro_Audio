using UnityEngine;

namespace Ami.Extension
{
    public class EnumSeparatorIndex : PropertyAttribute
    {
        public readonly int[] IndexList;

        public EnumSeparatorIndex(params int[] indexList)
        {
            IndexList = indexList;
        }
    }
}