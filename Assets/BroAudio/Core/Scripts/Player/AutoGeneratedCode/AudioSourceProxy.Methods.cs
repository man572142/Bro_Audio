// Auto-generated code
using UnityEngine;
using static Ami.Extension.FlagsExtension;

namespace Ami.Extension
{
    public partial class AudioSourceProxy : BroModifier<AudioSource>, IAudioSourceProxy
    {
        private int _modifiedCurveFlags = 0;
        private bool _hasCurveResetAction = false;
        public AnimationCurve GetCustomCurve(AudioSourceCurveType type) => Base.GetCustomCurve(type);

        public void SetCustomCurve(AudioSourceCurveType type, AnimationCurve curve)
        {
            Base.SetCustomCurve(type, curve);
            AddFlag(ref _modifiedCurveFlags, 1 << (int)type);

            AddResetAction(ref _hasCurveResetAction, ResetModifiedCurve);
        }

        private void ResetModifiedCurve()
        {
            int flag = 1;
            while (_modifiedCurveFlags != 0)
            {
                if (_modifiedCurveFlags.ContainsFlag(flag))
                {
                    ResetCurve((AudioSourceCurveType)flag);
                    RemoveFlag(ref _modifiedCurveFlags, flag);
                }
                flag <<= 1;
            }

            void ResetCurve(AudioSourceCurveType type)
            {
                switch (type)
                {
                    case AudioSourceCurveType.CustomRolloff:
                        Base.rolloffMode = AudioRolloffMode.Logarithmic;
                        break;
                    case AudioSourceCurveType.SpatialBlend:
                        Base.spatialBlend = 0f;
                        break;
                    case AudioSourceCurveType.ReverbZoneMix:
                        Base.reverbZoneMix = 1f;
                        break;
                    case AudioSourceCurveType.Spread:
                        Base.spread = 0f;
                        break;
                }
            }
        }
    }
}
