using System.Collections;
using Ami.BroAudio.Runtime;
using Ami.BroAudio.Tools;
using Ami.Extension;
using NUnit.Framework;

namespace Ami.BroAudio.Tests.Scenarios.Effects
{
    /// <summary>
    /// BroAudio.SetEffect(effect) drives the shared "Effect" mixer track directly through
    /// EffectAutomationHelper.SetEffectTrackParameter — no player needs to exist or be playing, since this is a
    /// mixer-only operation (see Runtime/DataStruct/Effect.cs and EffectAutomationHelper.GetEffectParameterName).
    /// A fadeTime of 0 collapses EffectAutomationHelper's per-frame Tweak() loop (its `while (currentTime &lt;
    /// fadeTime)` never executes) into a single synchronous AudioMixer.SetFloat call, so the target value is
    /// already applied by the time SetEffect() returns. The `yield return null` here isn't waiting out a fade;
    /// it's just staying consistent with the "always yield a frame before touching the mixer" rule from the
    /// engine facts (AudioMixer.SetFloat silently fails in Awake/OnEnable on the very first Play Mode frame).
    /// </summary>
    internal sealed class SetEffectLowPassScenario : IVerificationScenario
    {
        public string Description => "BroAudio.SetEffect(Effect.LowPass(...)) moves the shared LowPass mixer parameter, and ResetLowPass restores it";
        public string ExpectedOutcome => "The LowPass parameter starts at AudioConstant.MaxFrequency, moves to ~500Hz, then returns to AudioConstant.MaxFrequency after reset";

        public IEnumerator Run(VerificationContext context)
        {
            SoundManager.Instance.AudioMixer.GetFloat(BroName.LowPassParaName, out float baseline);
            Assert.AreEqual(AudioConstant.MaxFrequency, baseline, 0.5f, "LowPass should start at its default (effectively off) frequency.");

            BroAudio.SetEffect(Effect.LowPass(500f, 0f));
            yield return null;

            SoundManager.Instance.AudioMixer.GetFloat(BroName.LowPassParaName, out float applied);
            Assert.AreEqual(500f, applied, 1f, "SetEffect(Effect.LowPass(500f, 0f)) should move the LowPass parameter to ~500Hz.");

            BroAudio.SetEffect(Effect.ResetLowPass(0f));
            yield return null;

            SoundManager.Instance.AudioMixer.GetFloat(BroName.LowPassParaName, out float restored);
            Assert.AreEqual(AudioConstant.MaxFrequency, restored, 0.5f, "ResetLowPass should return the LowPass parameter to its default frequency.");
        }
    }

    /// <summary>
    /// The BroAudioType-scoped overload (BroAudio.SetEffect(effect, audioType)) still drives the very same
    /// shared HighPass mixer parameter as the global overload — SoundManager.SetEffect(targetType, effect)
    /// only uses targetType to flag which audio-type players get routed onto/off the Effect track for future
    /// plays (SetPlayerEffect); the mixer parameter itself is set once, through the same non-dominator
    /// EffectAutomationHelper, regardless of targetType. So the observable mixer-parameter assertions here are
    /// identical in shape to the global LowPass scenario, just for HighPass and via the 2-arg overload.
    /// </summary>
    internal sealed class SetEffectByTypeScenario : IVerificationScenario
    {
        public string Description => "BroAudio.SetEffect(Effect.HighPass(...), BroAudioType.SFX) moves the shared HighPass mixer parameter, and ResetHighPass restores it";
        public string ExpectedOutcome => "The HighPass parameter starts at AudioConstant.MinFrequency, moves to ~3000Hz, then returns to AudioConstant.MinFrequency after reset";

        public IEnumerator Run(VerificationContext context)
        {
            SoundManager.Instance.AudioMixer.GetFloat(BroName.HighPassParaName, out float baseline);
            Assert.AreEqual(AudioConstant.MinFrequency, baseline, 0.5f, "HighPass should start at its default (effectively off) frequency.");

            BroAudio.SetEffect(Effect.HighPass(3000f, 0f), BroAudioType.SFX);
            yield return null;

            SoundManager.Instance.AudioMixer.GetFloat(BroName.HighPassParaName, out float applied);
            Assert.AreEqual(3000f, applied, 1f, "SetEffect(Effect.HighPass(3000f, 0f), BroAudioType.SFX) should move the HighPass parameter to ~3000Hz.");

            BroAudio.SetEffect(Effect.ResetHighPass(0f), BroAudioType.SFX);
            yield return null;

            SoundManager.Instance.AudioMixer.GetFloat(BroName.HighPassParaName, out float restored);
            Assert.AreEqual(AudioConstant.MinFrequency, restored, 0.5f, "ResetHighPass should return the HighPass parameter to its default frequency.");
        }
    }
}
