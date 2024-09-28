// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : BroModifier<AudioSource>, IAudioSourceProxy
    {
        public AnimationCurve GetCustomCurve(AudioSourceCurveType type) => Base.GetCustomCurve(type);

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve) => Base.SetCustomCurve(type, curve);

        public bool GetAmbisonicDecoderFloat(int index, out float value) => Base.GetAmbisonicDecoderFloat(index, out value);

        public bool SetAmbisonicDecoderFloat(int index, float value) => Base.SetAmbisonicDecoderFloat(index, value);

        public bool GetSpatializerFloat(int index, out float value) => Base.GetSpatializerFloat(index, out value);

        public bool SetSpatializerFloat(int index, float value) => Base.SetSpatializerFloat(index, value);

    }
}
