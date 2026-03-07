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

        /// <summary>
        /// The sequence identifier for named sequence instances. Null means the default sequence.
        /// </summary>
        public string SequenceId { get; set; }

        public ClipSelectionContext(int value)
        {
            Value = value;
            SequenceId = null;
        }

        public static implicit operator ClipSelectionContext(int value) { return new ClipSelectionContext(value); }
    }
}
