// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : BroModifier<AudioSource>, IAudioSourceProxy
    {
        public AnimationCurve GetCustomCurve(AudioSourceCurveType type) => Base.GetCustomCurve(type);

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve) => Base.SetCustomCurve(type, curve);

    }
}
