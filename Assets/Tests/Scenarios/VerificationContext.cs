using System;

namespace Ami.BroAudio.Tests.Scenarios
{
    /// <summary>
    /// Carried into every <see cref="IVerificationScenario"/> run. The PlayMode runner leaves <see cref="Log"/>
    /// at its no-op default; the future QA-board scene (Layer 2) can supply one that writes to its on-screen
    /// caption instead, without scenarios needing to know which runner is driving them.
    /// </summary>
    internal sealed class VerificationContext
    {
        internal Action<string> Log = _ => { };
    }
}
