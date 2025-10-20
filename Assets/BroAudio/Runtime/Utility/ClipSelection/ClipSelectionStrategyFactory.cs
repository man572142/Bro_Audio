using System.Collections.Generic;
using Ami.BroAudio.Data;

namespace Ami.BroAudio.Runtime
{
    public static class ClipSelectionStrategyFactory
    {
        private static readonly Dictionary<MulticlipsPlayMode, IClipSelectionStrategy> _strategies = 
            new Dictionary<MulticlipsPlayMode, IClipSelectionStrategy>
            {
                { MulticlipsPlayMode.Single, new SingleClipStrategy() },
                { MulticlipsPlayMode.Sequence, new SequenceClipStrategy() },
                { MulticlipsPlayMode.Random, new RandomClipStrategy() },
                { MulticlipsPlayMode.Shuffle, new ShuffleClipStrategy() },
                { MulticlipsPlayMode.Chained, new ChainedClipStrategy() },
                { MulticlipsPlayMode.Velocity, new VelocityClipStrategy() },
            };
        
        public static IClipSelectionStrategy GetClipSelectionStrategy(this MulticlipsPlayMode playMode)
        {
            return _strategies.TryGetValue(playMode, out var strategy) 
                ? strategy 
                : _strategies[MulticlipsPlayMode.Single]; // Default to single if mode not found
        }
    }
}