using UnityEngine;

namespace Ami
{
    public enum FilterSlope
    {
#if UNITY_2019_2_OR_NEWER
        [InspectorName("12dB \u2215 Oct")]
#endif
        TwoPole,
#if UNITY_2019_2_OR_NEWER
        [InspectorName("24dB \u2215 Oct")]
#endif
        FourPole,
    }
}