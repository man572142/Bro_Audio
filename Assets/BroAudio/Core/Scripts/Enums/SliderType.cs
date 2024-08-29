using UnityEngine;

namespace Ami.BroAudio.Tools
{
    public enum SliderType
    {
        Linear,
        Logarithmic,
        BroVolume,

        [InspectorName(null)]
        BroVolumeNoField,
    } 
}
