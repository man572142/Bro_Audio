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
- [ ] `Play(id)` — basic playback, correct clip/volume/mixer group
- [ ] `Play(id, fadeIn)` — fade-in curve shape
- [ ] `Play(id, position)` — world position, spatial blend
- [ ] `Play(id, followTarget)` — follows transform; behavior when target is destroyed
- [ ] `IPlayableValidator` accepted/rejected paths

### Stop / Pause / UnPause
- [ ] `Stop(id)` / `Stop(id, fadeOut)` — fade-out shape, player recycled after
- [ ] `Stop(audioType)` / `Stop(audioType, fadeOut)` — only matching type stops
- [ ] `Pause` / `UnPause` by id and by type, with and without fade; `timeSamples` preserved across pause
- [ ] Stop during an in-progress fade-in (no leak, no stuck volume)

### Volume / Pitch
- [ ] `SetVolume` global / by type / by id, immediate and faded (correct domain: source vs. mixer)
- [ ] `SetPitch` global / by type / by id, immediate and faded
- [ ] Explicit pitch 1 overrides pitch randomization (pins the fix in 529b085)
- [ ] `IAudioPlayer.SetVolume` / `SetPitch` chaining on a live player

### Chaining / decorators
- [ ] `AsBGM()` + `SetTransition` — every `Transition` × `StopMode` combination, override fade
- [ ] `AsDominator()` — `QuietOthers`, `LowPassOthers`, `HighPassOthers` (mixer params move and restore)
- [ ] `SetScheduledStartTime` / `SetScheduledEndTime` — dspTime accuracy
- [ ] `SetDelay` — including `BroAudioClip.Delay` field interaction
- [ ] `SetVelocity` — selects the right velocity layer
- [ ] `SetSequenceId` + `ResetMultiClipStrategy(id, sequenceId)`
- [ ] `OnAudioFilterRead` callback receives buffers

### Clip selection strategies (one scenario each)
- [ ] Single
- [ ] Random
- [ ] Sequence (order over repeated plays; `ResetMultiClipStrategy` restarts it)
- [ ] Shuffle (no repeats within a round)
- [ ] Velocity
- [ ] Chained
- [ ] Layered
- [ ] Localization (behind `PACKAGE_LOCALIZATION`)

### Looping
- [ ] Normal loop — keeps playing past clip length
- [ ] Seamless loop — state-level scheduling asserts (Layer 1) + gaplessness capture (Layer 3)

### Playback groups
- [ ] Max-instance cap enforced
- [ ] Cooldown / interval rules enforced

### Effects
- [ ] `SetEffect(effect)` global and by type — mixer params reach target
- [ ] Auto-reset waitable restores previous values

### MonoComponents
- [ ] `SoundSource` — play on enable/API, stop behavior
- [ ] `SoundVolume` — applies configured volumes
- [ ] `SpectrumAnalyzer` — produces spectrum data while playing

### Lifecycle / infrastructure
- [ ] `Init()` and the `BroAudio_InitManually` path
- [ ] Pool integrity — N concurrent plays, all recycled to baseline
- [ ] Teardown — release verbs during `OnDestroy`/`OnApplicationQuit` don't throw
- [ ] `HasAnyPlayingInstances`, `TryGetEntityInfo`

### Optional packages (behind defines, in split scenario files)
- [ ] Addressables: `IsLoaded`, `ReleaseAsset`, `ReleaseAllAssets` (`PACKAGE_ADDRESSABLES`)
- [ ] Localization: `Subscribe`/`UnsubscribeLocalizedAudioChanged` (`PACKAGE_LOCALIZATION`)
- [ ] Suite compiles with both packages **absent** (same partial-file + `#if` rule as runtime)

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
