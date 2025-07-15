namespace Ami.BroAudio
{
    /// <summary>
    /// Context class containing parameters required for clip selection
    /// </summary>
    public struct ClipSelectionContext
    {
        /// <summary>
        /// The ID of the audio entity
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The velocity value used for velocity-based selection
        /// </summary>
        public int Value { get; set; }
    }
}
