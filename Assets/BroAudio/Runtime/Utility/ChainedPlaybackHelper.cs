namespace Ami.BroAudio.Runtime
{
    /// <summary>
    /// Pure-function helper that centralizes the chained multiclip playback state machine rules.
    /// Both the Runtime (<see cref="PlaybackPreference"/>) and Editor (<c>EntityReplayRequest</c>) delegate here.
    /// </summary>
    public static class ChainedPlaybackHelper
    {
        public static PlaybackStage InitialStage => PlaybackStage.Start;

        public static bool CanHandoverToLoop(PlaybackStage currentStage, bool isSeamlessLoop)
        {
            if (currentStage == PlaybackStage.End || currentStage == PlaybackStage.None)
            {
                return false;
            }
            return isSeamlessLoop || currentStage == PlaybackStage.Start;
        }

        public static bool CanHandoverToEnd(PlaybackStage currentStage, int clipCount)
        {
            if (clipCount < (int)PlaybackStage.End)
            {
                return false;
            }
            return currentStage != PlaybackStage.End && currentStage != PlaybackStage.None;
        }

        public static bool CanContinueLooping(PlaybackStage currentStage)
        {
            return currentStage == PlaybackStage.Loop;
        }

        public static bool CanReplay(PlaybackStage currentStage, int clipCount)
        {
            return (int)currentStage <= (int)PlaybackStage.End && clipCount > (int)currentStage - 1;
        }

        public static PlaybackStage AdvanceToLoop()
        {
            return PlaybackStage.Loop;
        }

        public static PlaybackStage AdvanceToEnd()
        {
            return PlaybackStage.End;
        }
    }
}
