using Ami.BroAudio.Data;

namespace Ami.BroAudio
{
    /// <summary>
    /// Strategy interface for selecting audio clips based on different selection algorithms
    /// </summary>
    public interface IClipSelectionStrategy
    {
        /// <summary>
        /// Selects a clip from the array using the specific strategy
        /// </summary>
        /// <param name="clips">Array of clips to select from</param>
        /// <param name="context">Selection context containing parameters for the selection</param>
        /// <param name="index">Output index of the selected clip</param>
        /// <returns>The selected clip</returns>
        IBroAudioClip SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index);
    }
}