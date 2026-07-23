using System.Collections;

namespace Ami.BroAudio.Tests.Scenarios
{
    /// <summary>
    /// A single characterization scenario: a description, its expected observable outcome, and the coroutine
    /// that drives it and asserts on it. This is the single source of truth described in VERIFICATION_PLAN.md
    /// guiding principle #3 — today it's consumed only by the Layer 1 PlayMode suite (<c>Assets/Tests/PlayMode</c>),
    /// but it's shaped so the Layer 2 QA-board scene can reuse the same scenarios later without duplicating them.
    /// </summary>
    internal interface IVerificationScenario
    {
        string Description { get; }
        string ExpectedOutcome { get; }
        IEnumerator Run(VerificationContext context);
    }
}
