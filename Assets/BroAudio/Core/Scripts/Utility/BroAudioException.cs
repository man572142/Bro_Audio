using System;

namespace Ami.BroAudio.Runtime
{
    public class BroAudioException : Exception
    {
        public BroAudioException(string message) : base(Utility.LogTitle + message)
        {
        }
    } 
}