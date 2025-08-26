// Auto-generated code
using UnityEngine;
using System;

namespace Ami.Extension
{
    public interface IAudioLowPassFilterProxy
    {
        /// <inheritdoc cref="AudioLowPassFilter.customCutoffCurve"/>
        AnimationCurve customCutoffCurve { get; set; }

        /// <inheritdoc cref="AudioLowPassFilter.cutoffFrequency"/>
        float cutoffFrequency { get; set; }

        /// <inheritdoc cref="AudioLowPassFilter.lowpassResonanceQ"/>
        float lowpassResonanceQ { get; set; }

        /// <inheritdoc cref="AudioLowPassFilter.enabled"/>
        bool enabled { get; set; }

    }
}
