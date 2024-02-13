using System;

namespace Ami.BroAudio.Runtime
{
    public class Waiter
    {
        public bool IsFinished { get; private set; }

        public void Finish()
        {
            IsFinished = true;
        }
    }
}