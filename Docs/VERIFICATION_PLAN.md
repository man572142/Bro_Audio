# Refactoring Verification Plan

A safety net to build **before** the upcoming architectural refactoring, so that behavior
changes are detected instead of discovered by users. The strategy has three layers, ordered
by return on investment, all sharing a single set of scenario definitions.

## Why not just a listening scene?

Ears catch "sound doesn't play" but miss what refactorings actually break: a fade curve
silently switching from eased to linear, a volume landing at 0.72 instead of 0.8, a player
leaking from the pool, a seamless loop drifting 30 ms, a stop callback that no longer fires.
The primary safety net must therefore be **automated characterization tests**; the audible
scene is a second layer for the things only ears can judge (crossfade feel, gaplessness,
spatialization).

## Guiding principles

1. **Test the public contract, not the internals.** All scenarios drive the system through
   `BroAudio.*`, the `IAudioPlayer` chain, and the MonoComponents, with a real `SoundManager`
   booted from `Resources/SoundManager.prefab`. Internals-level tests would be demolished by
   the refactor itself and can't answer "did behavior change?". This also sidesteps the known
   construction constraints (`PlaybackPreference` ctor needs a live `SoundManager`;
   `AudioPlayer` needs an `IAudioMixerPool`).
2. **Characterize, don't correct.** Tests assert what the code does *today*, even where
   today's behavior looks wrong. Bugs found while writing tests get pinned with a
   `// current behavior, revisit` note and an entry in `Docs/KNOWN_BEHAVIOR_QUIRKS.md` —
   they are not fixed mid-snapshot, otherwise refactor regressions and intentional changes
   become indistinguishable.
3. **One scenario list, two runners.** Each scenario is a plain class (description, expected
   observable outcome, `Run(context)`), consumed by both the PlayMode test suite and the
   QA-board scene UI. One feature list, no double maintenance, coverage gaps visible in one
   place.
4. **Green on `main` before the first refactor commit.** The suite is only a baseline if it
   passes on the current architecture first.

## Layer 1 — Facade-level PlayMode characterization tests (primary safety net)

Unity Test Framework PlayMode tests under `Assets/Tests/` (new asmdef referencing
`BroAudio`). Run from the Test Runner window, or headless:

```
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml -quit
```

This CLI run is the poor-man's CI: run it before every refactor commit.

### What is assertable without ears

| Aspect | How to observe |
|---|---|
| Playback state | `AudioSource.isPlaying`, `clip`, `timeSamples`, `outputAudioMixerGroup` |
| Volume/pitch | `AudioSource.volume` (linear domain), `pitch`; mixer params via `AudioMixer.GetFloat` (dB domain) |
| Fade **shape** | Sample `volume` across several frames during a fade; assert curve points within tolerance — catches "the ease changed" |
| Positioning | Transform position, follow-target parenting, `spatialBlend` |
| Effects / dominator | Mixer parameter values (ducking volume, low/high-pass cutoff), and their restoration after stop / auto-reset |
| Scheduling | `SetDelay` / `SetScheduledStartTime` / `SetScheduledEndTime` against `AudioSettings.dspTime`; assert on `timeSamples` progression, **not** `isPlaying` (see engine facts below) |
| Clip selection | The sequence of selected clips over repeated plays (each fixture clip identifiable — see Fixtures) |
| Pooling | Active-player count returns to baseline after N plays; no leak through stop-during-fade, pause→unpause, or decorator paths |
| Callbacks | `OnEnd`-style callbacks fire exactly once, at the expected time |
| Queries | `HasAnyPlayingInstances`, `TryGetEntityInfo`, `IsLoaded` |
| Teardown | Release verbs during `OnDestroy` / `OnApplicationQuit` are silent no-ops (the null-safe `BroAudio.Manager` path) |

### Engine facts that shape test code

From `.claude/rules/unity-audio-engine.md` — the suite must respect these or it will flake:

- `AudioMixer.SetFloat` silently fails in `Awake`/`OnEnable` on the first Play Mode frame →
  every test yields at least one frame after entering Play Mode before touching the mixer.
- `PlayScheduled` / `PlayDelayed` report `isPlaying == true` immediately → scheduling tests
  assert on `timeSamples` progression, not `isPlaying`.
- After a clip finishes, `timeSamples` rests at the start sample, not 0.
- `AudioSource.volume` is linear; only mixer parameters are dB — assert in the right domain.
- Batchmode caveat: `-batchmode` initializes audio with a dummy output device. State-based
  assertions are expected to work; verify once, and if any timing-sensitive subset behaves
  differently, mark it to run from the Test Runner window only.

## Layer 2 — Audible QA-board scene (manual ear/eye pass)

A dev-only scene, **checklist not showcase** (the existing `Samples~/Demo` is an experience,
not an audit):

- Flat grid: one row per scenario — a button plus a label stating the *expected* outcome,
  e.g. "BGM transition, CrossFade 2 s: no gap, no dip", "Dominator: others duck + muffle,
  restore on stop".
- **Run All** mode: steps through every scenario automatically with an on-screen caption of
  what should be heard.
- Live state HUD: active player count, current mixer parameter values, per-player
  `isPlaying`/volume — state discrepancies become visible even when inaudible.
- **Baseline recording:** before the refactor, record a full Run All pass (system audio
  capture is fine) and commit/store it. After each refactor milestone, re-run and A/B
  against the recording — comparing two recordings is something ears do far better than
  judging one run from memory.

Cadence: automated suite on every refactor commit; ear pass at milestones only.

## Layer 3 — Targeted golden-master audio capture (optional, scoped)

For behaviors that state assertions genuinely can't see:

- **Seamless loop gaplessness**
- **BGM transition sum** (crossfade should not dip or clip)

Capture DSP output via the existing tap (`AudioFilterReader` / `IAudioPlayer.OnAudioFilterRead`)
or `AudioRenderer.Start()`/`Render()` for the master output. Compare **RMS envelopes over
windows and onset timings against a stored baseline — never raw samples** (DSP timing
jitters run-to-run). Deliberately scoped to these two behaviors; blanket golden-master audio
is brittle and high-maintenance.

## Fixtures

- **Procedural clips**, not committed wavs: `AudioClip.Create` with sine tones at distinct
  frequencies and exact lengths. Deterministic, and each entity is identifiable by frequency
  — which is what makes clip-selection sequences assertable and enables Layer 3 comparisons.
- **Test `AudioAsset`** with entities covering every clip-selection strategy, loop mode,
  delay, and velocity layering. Built in code via `ScriptableObject.CreateInstance` at
  setup (repo hooks block hand-editing `.asset` YAML), or authored once through the Library
  Manager.
- **Boot helper** that instantiates `SoundManager` from the prefab, waits the required first
  frame, and tears down cleanly between tests.

## Coverage matrix

Derived from the facade (`Runtime/BroAudio.cs`) and the `IAudioPlayer` chain interfaces.
Check items off as scenarios land.

### Play verbs
- [x] `Play(id)` — basic playback, correct clip/volume (mixer *group* assignment itself isn't independently asserted — `PlayBasicScenario`)
- [x] `Play(id, fadeIn)` — fade-in curve shape, via `GetVolume()` since the source routes through the mixer (`PlayFadeInScenario`)
- [x] `Play(id, position)` — spatial blend forced to 3D (world *position* isn't state-assertable — `IAudioPlayer` exposes no `Transform` — `PlaySpatialScenario`)
- [x] `Play(id, followTarget)` — spatial blend forced to 3D, and tolerates the target being destroyed (position-tracking itself isn't state-assertable for the same reason — `PlayFollowTargetScenario`)
- [ ] `IPlayableValidator` accepted/rejected paths — only exercised indirectly through `PlaybackGroup` as validator (see Playback groups below); a custom `IPlayableValidator` override isn't separately covered

### Stop / Pause / UnPause
- [x] `Stop(id)` / `Stop(id, fadeOut)` — fade-out shape via `GetVolume()`, player recycled after (`StopImmediateScenario`, `StopFadeOutScenario`)
- [ ] `Stop(audioType)` / `Stop(audioType, fadeOut)` — only used as test teardown, not scenario-verified that it's type-selective
- [x] `Pause` / `UnPause` by id, `timeSamples` preserved across pause (`PauseUnPauseScenario`; by-type and fade-time variants not separately covered)
- [x] Stop during an in-progress fade-in (no leak, no stuck volume) (`StopDuringFadeInScenario`)

### Volume / Pitch
- [x] `SetVolume` by id, immediate and faded (`SetVolumeScenario`; global/by-type paths not separately covered)
- [x] `SetPitch` by id, immediate and faded (`SetPitchScenario`; global/by-type paths not separately covered)
- [x] Explicit pitch 1 overrides pitch randomization (pins the fix in 529b085) (`ExplicitPitchOverridesRandomizationScenario`)
- [x] `IAudioPlayer.SetPitch` chaining on a live player (`ExplicitPitchOverridesRandomizationScenario`); `SetVolume` chaining specifically not covered (only the `BroAudio.SetVolume(id, ...)` facade path is)

### Chaining / decorators
- [ ] `AsBGM()` + `SetTransition` — only `Transition.Immediate` covered (`BgmTransitionScenario`, via `OnBGMChanged`); the full `Transition` × `StopMode` matrix and override-fade aren't
- [ ] `AsDominator()` — only `QuietOthers` covered, ducking/restoring the shared `Main_Dominated` mixer parameter (`DominatorQuietOthersScenario`); `LowPassOthers`/`HighPassOthers` aren't
- [x] `SetScheduledStartTime` — dspTime accuracy via `timeSamples` (`ScheduledStartTimeScenario`); `SetScheduledEndTime` not separately covered
- [x] `SetDelay` — including `BroAudioClip.Delay` field interaction (`SetDelayScenario`, `ClipDelayFieldScenario`)
- [x] `SetVelocity` — selects the right velocity layer (`VelocityClipSelectionScenario`)
- [x] `SetSequenceId` + `ResetMultiClipStrategy(id)` (`SequenceClipSelectionScenario`); the named-sequence-specific `ResetMultiClipStrategy(id, sequenceId)` overload isn't separately covered
- [ ] `OnAudioFilterRead` callback receives buffers

### Clip selection strategies (one scenario each)
- [x] Single (`SingleClipSelectionScenario`)
- [x] Random (`RandomClipSelectionScenario`, via a deterministic dominant-weight setup)
- [x] Sequence (order over repeated plays; `ResetMultiClipStrategy` restarts it — `SequenceClipSelectionScenario`)
- [x] Shuffle — **the matrix's "no repeats within a round" premise is itself false**; see `Docs/KNOWN_BEHAVIOR_QUIRKS.md`. `ShuffleClipSelectionScenario` asserts the actually-true property (every clip appears over enough plays) instead
- [x] Velocity (`VelocityClipSelectionScenario`)
- [x] Chained — only that playback starts on the intro clip (`ChainedClipSelectionScenario`); the automatic intro→loop→outro handover is explicitly out of scope (timing-fragile for Layer 1's state-level suite)
- [ ] Layered — **waived**: unreachable from `MulticlipsPlayMode` today, see `Docs/KNOWN_BEHAVIOR_QUIRKS.md`
- [ ] Localization (behind `PACKAGE_LOCALIZATION`) — not attempted

### Looping
- [x] Normal loop — keeps playing past clip length (`NormalLoopScenario`)
- [x] Seamless loop — state-level scheduling asserts (Layer 1 — `SeamlessLoopScenario`); gaplessness capture is Layer 3, out of scope here

### Playback groups
- [x] Max-instance cap enforced (`MaxInstanceCapScenario`)
- [x] Cooldown / interval rules enforced (`CombFilteringCooldownScenario`)

### Effects
- [x] `SetEffect(effect)` global and by type — mixer params reach target (`SetEffectLowPassScenario`, `SetEffectByTypeScenario`)
- [x] Auto-reset waitable restores previous values (covered by the Dominator `QuietOthers` scenario above, which uses the `.While(PlayerIsPlaying)` auto-reset path; the plain Effects scenarios use explicit `Reset*` calls instead)

### MonoComponents
- [x] `SoundSource` — play/stop via the public API (`SoundSourcePlaysAndStopsScenario`); the `_playOnEnable`/`_stopOnDisable`/`_onlyPlayOnce` inspector-authored toggles aren't covered
- [ ] `SoundVolume` — applies configured volumes
- [ ] `SpectrumAnalyzer` — produces spectrum data while playing

### Lifecycle / infrastructure
- [ ] `Init()` and the `BroAudio_InitManually` path — the suite relies on auto-bootstrap and calls `Init()` defensively in `SoundManagerTestContext`, but no scenario verifies the manual-init path itself
- [x] Pool integrity — N concurrent plays, all recycled to baseline (`PoolIntegrityScenario`)
- [ ] Teardown — release verbs during `OnDestroy`/`OnApplicationQuit` don't throw — **deliberately deferred**: `SoundManager` is a shared singleton across the whole Play Mode session, so destroying it mid-suite to exercise this path would break every other test; needs a dedicated, isolated setup
- [x] `HasAnyPlayingInstances`, `TryGetEntityInfo` (`QueriesScenario`)

### Optional packages (behind defines, in split scenario files)
- [ ] Addressables: `IsLoaded`, `ReleaseAsset`, `ReleaseAllAssets` (`PACKAGE_ADDRESSABLES`) — not attempted
- [ ] Localization: `Subscribe`/`UnsubscribeLocalizedAudioChanged` (`PACKAGE_LOCALIZATION`) — not attempted
- [x] Suite compiles with both packages **absent** — holds by construction: no `Assets/Tests/` code references Addressables or Localization types

## Placement

Everything lives **outside** `Assets/BroAudio/` — that subtree is exactly what ships.

```
Assets/Tests/
  BroAudio.Tests.asmdef          (references BroAudio; UTF + test defines)
  Scenarios/                     (shared scenario classes — the single source of truth)
  PlayMode/                      (NUnit wrappers driving the scenarios)
  Fixtures/                      (procedural clip + AudioAsset builders, boot helper)
  QABoard/                       (verification scene + UI scripts + HUD)
Docs/
  VERIFICATION_PLAN.md           (this file)
  KNOWN_BEHAVIOR_QUIRKS.md       (bugs pinned, not fixed, during characterization)
```

Note: creating the `Assets/Tests` asmdef is an "ask first" item per CLAUDE.md boundaries —
confirmed by this plan's approval.

## Order of operations

1. **Scaffold** — test asmdef, scenario framework (`IVerificationScenario` or similar),
   procedural-clip fixtures, test `AudioAsset` builder, `SoundManager` boot helper.
2. **Layer 1 tests** — implement the state-observable scenarios from the matrix. Get them
   **green on current `main`** and commit that baseline before touching architecture.
3. **Layer 2 scene** — QA board reusing the same scenarios; record the baseline audio pass.
4. **Layer 3 (optional)** — capture-based asserts for seamless loop and BGM transitions.
5. **Refactor in slices** — batchmode suite between slices; ear pass + A/B against the
   baseline recording at milestones; update `KNOWN_BEHAVIOR_QUIRKS.md` whenever a pinned
   behavior is intentionally changed (each such change flips a characterization test on
   purpose, in its own commit).

## Exit criteria

The safety net is "done" when:

1. Every unchecked matrix item above has a scenario, or an explicit written waiver.
2. The full PlayMode suite passes on current `main` via the batchmode CLI.
3. The QA-board Run All pass is recorded and stored as the audible baseline.
4. The suite compiles with Addressables and Localization absent.
