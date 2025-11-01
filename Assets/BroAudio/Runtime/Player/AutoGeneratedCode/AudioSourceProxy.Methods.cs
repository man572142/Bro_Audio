// Auto-generated code
using UnityEngine;
using UnityEngine.Audio;
using System;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : IDisposable, IAudioSourceProxy
    {
        public AnimationCurve GetCustomCurve(AudioSourceCurveType type) => _source.GetCustomCurve(type);

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve) => _source.SetCustomCurve(type, curve);

        public bool GetAmbisonicDecoderFloat(int index, out float value) => _source.GetAmbisonicDecoderFloat(index, out value);

        public bool SetAmbisonicDecoderFloat(int index, float value) => _source.SetAmbisonicDecoderFloat(index, value);

        public bool GetSpatializerFloat(int index, out float value) => _source.GetSpatializerFloat(index, out value);

        public bool SetSpatializerFloat(int index, float value) => _source.SetSpatializerFloat(index, value);

    }
}
