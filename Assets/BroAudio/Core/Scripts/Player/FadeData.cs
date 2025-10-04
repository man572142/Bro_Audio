using Ami.Extension;

namespace Ami.BroAudio.Runtime
{
    public struct FadeData
    {
        private Ease _baseEase;
        private Ease _nextEase;
        public const float UseClipSetting = -1f;
        public const float Immediate = 0f;
        
        public float Base { get; set; }
        public float Next { get; set; }

        public FadeData(Ease ease, Ease nextEase)
        {
            Base = UseClipSetting;
            _baseEase = ease;
            Next = UseClipSetting;;
            _nextEase = nextEase;
        }

        public bool TryGetOrConsumeOverride(out float fade, out Ease ease)
        {
            if (Next >= Immediate)
            {
                fade = Next;
                Next = UseClipSetting;
                ease = _nextEase;
                return true;
            }
            fade = Base;
            ease = _baseEase;
            return fade >= Immediate;
        }
    }
}