using UnityEngine;

namespace Ami.BroAudio
{
    public enum FilterSlope
    {
        [InspectorName("12dB \u2215 Oct")]
        TwoPole,
        [InspectorName("24dB \u2215 Oct")]
        FourPole,
    }
}