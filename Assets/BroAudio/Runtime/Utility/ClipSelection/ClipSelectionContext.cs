using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Context class containing parameters required for clip selection
    /// </summary>
    public struct ClipSelectionContext
    {
        /// <summary>
        /// The velocity value used for velocity-based selection
        /// </summary>
        public int Value { get; set; }

        public ClipSelectionContext(int value)
        {
            Value = value;
        }

        public static implicit operator ClipSelectionContext(int value) { return new ClipSelectionContext(value); }
    }
}
