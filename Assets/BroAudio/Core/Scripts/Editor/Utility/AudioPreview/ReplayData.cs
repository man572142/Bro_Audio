using System;

namespace Ami.BroAudio.Editor
{
    public struct ReplayData
    {
        public bool IsReplaying;
        public Action<ReplayData> OnReplay;
    }
}