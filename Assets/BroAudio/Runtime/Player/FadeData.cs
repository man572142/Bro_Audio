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
            Next = UseClipSetting;
            _nextEase = nextEase;
        }

        public void SetEase(Ease ease)
        {
            _nextEase = ease;
            _baseEase = ease;
        }

        public bool HasPendingOverride => Next >= Immediate;

        // Non-consuming resolve that mirrors TryGetOrConsumeOverride's priority (pending override > base > clip setting),
        // so callers can decide the starting volume without consuming the one-shot override.
        public float ResolveFade(float clipFade) => HasPendingOverride ? Next : (Base >= Immediate ? Base : clipFade);

        public bool TryGetOrConsumeOverride(out float fade, out Ease ease)
        {
            if (Next >= Immediate)
            {
                fade = Next;
                // consume the override
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