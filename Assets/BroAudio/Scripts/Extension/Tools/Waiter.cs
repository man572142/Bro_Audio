using System;

namespace Ami.BroAudio.Runtime
{
    public class Waiter
    {
        public event Action OnFinished;
        public bool IsFinished { get; private set; }

        public void Finish()
        {
            IsFinished = true;
        }
    }
}