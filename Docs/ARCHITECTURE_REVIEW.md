# BroAudio — Architecture Review

*Scope: full codebase under `Assets/BroAudio/` (runtime + editor, ~31.7k lines of C# across 306 files) as of v3.2.2 on `DEV_Unity6`.*

---

## 1. Overall Assessment

BroAudio is a mature, feature-rich audio middleware with several genuinely strong architectural decisions. The review below is weighted toward what should change, so it's worth stating clearly what is working:

**Strengths**

- **Clean assembly split.** Two asmdefs (`BroAudio` runtime, `BroAudioEditor` editor-only) with correct platform gating. Optional dependencies (Addressables, Localization) are handled via `versionDefines` + partial files suffixed `.Addressables.cs` / `.Localization.cs` — the package genuinely compiles with those packages absent, which many Unity packages get wrong.
- **Package-as-source layout.** `Assets/BroAudio/` *is* the shipped artifact for both UPM and Asset Store channels, with dev-only tooling compiled out behind `BroAudio_DevOnly`. One source of truth, no sync step.
- **Sound patterns in the right places.** Object pooling for players and mixer tracks; strategy pattern for clip selection (`Runtime/Utility/ClipSelection/`); decorators for behavior modes (`MusicPlayer`, `DominatorPlayer`); a segregated public API (`IAudioPlayer` composed of `IVolumeSettable`, `ISchedulable`, `IAudioStoppable`, …); a static facade with a deliberate, documented null-safety asymmetry between play and release verbs for teardown ordering.
- **DSP-clock-based scheduling** rather than frame-time arithmetic for loop seams and scheduled playback — the correct foundation for gapless audio.
- **Empirically documented engine behavior** (`.claude/rules/unity-audio-engine.md`). Hand-verified engine facts that contradict Unity's docs are recorded and cited in code. This is rare and valuable.

**Core weaknesses**

The dominant architectural problem is that **almost nothing in the runtime can execute without a fully bootstrapped `SoundManager` singleton**, and there is **no automated test suite** (no `Assets/Tests/`, no test asmdef, no CI test job). These two facts reinforce each other: the singleton coupling makes tests hard to write, and the absence of tests makes the coupling expensive to unwind. Meanwhile the most intricate subsystem — DSP-scheduled playback with seamless-loop player handover — is exactly the kind of code that regresses without tests, and recent git history (pause/resume NRE fix, double-teardown fix, pitch-fade snapping fix, mid-play pitch end-time recompute) shows that regression-chasing is already the normal maintenance mode.

---

## 2. Detailed Findings

### 2.1 Structure & modularity

| Area | Observation |
|---|---|
| Assemblies | 2 asmdefs, correct direction (`BroAudioEditor` → `BroAudio`), editor-only platform gate. Good. |
| Namespaces | `Ami.BroAudio` / `.Runtime` / `.Data` / `.Tools` / `Ami.Extension` — logical, though `Data` types depend on `Runtime` (see 2.3). |
| God classes | `SoundManager` spans 9 partial files (~1,600 lines) and at least eight responsibilities: singleton bootstrap, player pool, two mixer-track pools, playback queue, volume/pitch orchestration, effect automation, Addressables lifetime GC, Localization cache, comb-filtering tracking, legacy-ID conversion. `AudioPlayer` spans 8 partials plus generated proxies. Partial files hide file length but not coupling — every partial shares the same private state, so the unit of reasoning is still the whole class. |
| Editor | The editor half is larger than the runtime by file count. `LibraryManagerWindow` (883 lines), `AudioEntityEditor` (846 + 583), `ReorderableClips` (747 + a 773-line Localization partial) are IMGUI monoliths, but they are leaf code — lower risk than the runtime issues. |

### 2.2 The singleton graph

`SoundManager.Instance` / `HasInstance` / static ease properties are referenced **51 times across 16 runtime files**, including from layers that should not know a manager exists:

- `AudioEntity.HasLoop()` (a *data* ScriptableObject) reads `SoundManager.Instance.Setting` for chained-mode defaults.
- `PlaybackPreference`'s constructor reads `SoundManager.FadeInEase` / `FadeOutEase` — constructing a plain struct requires a live scene singleton.
- `SoundID` extension methods (`GetAudioClip`, `HasAnyPlayingInstances`, Addressables loaders) call `SoundManager.Instance` directly.
- `AudioPlayer` holds `IAudioMixerPool Mixer => SoundManager.Instance;` — the abstraction exists but is re-bound to the singleton at the use site, and `AudioPlayer.Playback` additionally reaches for `Instance.ScheduledPlaybackWarmUpTime`, `Instance.Setting`, `Instance.TryGetAudioTypePref` directly.
- `Fader`, `AudioPlayerInstanceWrapper`, `DominatorPlayer`, `PlaybackGroup` defaults all touch the singleton.

Consequences: nothing is unit-testable; teardown ordering requires defensive null-guards scattered through the code (`Fader.StopCoroutine`, `BroAudio.Manager`, `CanHandoverToLoop`'s torn-down-pref guard); and any future scenario with more than one audio context (server builds, additive-scene sandboxing, editor preview using runtime code) is structurally impossible.

### 2.3 Layering inversion

`Ami.BroAudio.Data` (entities, clips, IDs) depends upward on `Ami.BroAudio.Runtime` (`SoundManager`, `Utility`). Data types should be inert; today they embed playback policy (`AudioEntity.HasLoop` pulling runtime settings) and service location (`SoundID` extensions). This is why supplying an `IAudioMixerPool` test double (as CLAUDE.md suggests for driving `AudioPlayer`) is not actually sufficient — the data objects themselves call home.

### 2.4 The handover protocol (highest defect density)

Seamless loops and chained mode are implemented by **handing playback to a freshly extracted `AudioPlayer` at the seam**, with `AudioPlayerInstanceWrapper` keeping user-held references valid. The state that must survive the seam is transferred manually, field by field:

- `PlaybackHandoverData` carries ID, pref, clip, track effect, pitch, and three conditional track-volume-fade fields.
- `AudioPlayerInstanceWrapper.UpdateInstance` must call `TransferOnUpdates`, `TransferOnEnds`, `TransferOnPauses`, `TransferDecorators`, `TransferAddedEffectComponents` (which re-`AddComponent`s filter components and copies their values).
- `MusicPlayer.UpdateInstance` must silently re-point its **static** `_currentBGMPlayer` field.

This is an *implicit contract*: any new stateful feature on `AudioPlayer` must remember to join the transfer protocol, or its state silently disappears at the first loop seam. The density of invariant-explaining comments in `AudioPlayer.Playback.cs` ("keep the ce15a806 invariant", "BeginHandover only nulls _instanceWrapper on success…") and the recent bug-fix history both mark this as the code most likely to regress again.

### 2.5 Coroutine-based timing engine

All playback control, fading, and effect automation run as Unity coroutines polling per frame (`while (AudioSettings.dspTime < end) yield return null;`). `PlayControl()` in `AudioPlayer.Playback.cs` is a ~160-line coroutine interleaving seven concerns: audio-type volume sync, clip selection, Addressables await, resume-vs-fresh setup, schedule resolution, BGM transition wait, fade-in, seamless-handover scheduling, fade-out, end detection. The timing *math* (`ResolveScheduledTiming`, pause rebasing, pitch-adjusted durations, seam start computation) is correct-looking but is embedded in coroutine flow, so it can only ever be exercised in Play Mode with real time passing.

Cost profile is acceptable for typical voice counts (the per-frame work per player is small), but the design costs are: per-play allocations (coroutines, `AudioPlayerInstanceWrapper` per `Play()` call, `GetInvocationList()` arrays on every handover), fragile teardown, and untestability.

### 2.6 Legacy debt in hot paths

- `SoundID.Entity` runs `_fixLegacyId()` (obsolete-suppressed) on **every access** when `_entity` is null.
- `SoundIDExtension.TryConvertIdToEntity` reaches from the **runtime assembly into the editor assembly by reflection string** (`Type.GetType("Ami.BroAudio.Editor.BroEditorUtility, BroAudioEditor")`). Editor-only path, but rename-fragile and invisible to the compiler.
- 38 `[Obsolete]` members across the codebase, several still `[SerializeField]` inside core types (`SoundManager._data`, `AudioEntity.ID`, `BroAudioData` whole class), plus `AudioEntity_LEGACY_DEPRECATED.cs`.

Migration support is a legitimate product requirement (Asset Store users upgrade from old data), but today it lives *inside* the core types rather than in a versioned upgrade pipeline that runs once.

### 2.7 Conditional-compilation matrix

86 `#if` directives in the runtime across five independent dimensions: `UNITY_EDITOR` (24), `PACKAGE_LOCALIZATION` (15), `UNITY_WEBGL` (21 combined), `PACKAGE_ADDRESSABLES` (14+), `BroAudio_InitManually`, plus console defines. The partial-file convention contains Addressables/Localization well, but **WebGL forks are inline** — `SoundManager.SetMasterVolume` contains a complete duplicate of the volume-fade loop for WebGL. No CI compiles any of these combinations, so "compiles on my machine, breaks with package X absent / on platform Y" is only caught manually (the project's own Definition of Done acknowledges this).

### 2.8 Smaller observations

- `AudioPlayerObjectPool.GetCurrentAudioPlayers()` returns the live mutable list; `SoundManager.Stop`/`Pause` iterate it backwards defensively (players recycle synchronously), but `SetVolume`/`SetPitch`/`SetPlayerEffect`/`HasAnyPlayingInstances` iterate it with `foreach`. The invariant "these operations never synchronously recycle a player" is real but implicit — one future change to `SetVolume(0)` semantics away from an `InvalidOperationException`.
- `_combFilteringPreventer` keeps only the latest player per `SoundID`; fine for its purpose, but it lives in `SoundManager` while the rule that consumes it lives in `PlaybackGroup` — split-brain feature placement.
- `EffectAutomationHelper` implements its own decorator/waitable hierarchy for tweening mixer parameters — a second, parallel fading system next to `Fader`. Consolidation candidate.
- Editor↔runtime string-name coupling to the mixer asset (`BroName` track/parameter names) is centralized — good — but the mixer asset itself is a hand-authored artifact whose parameter set must stay in sync with code expectations; nothing validates that at import/CI time.

---

## 3. Top Five Recommendations

Ordered by long-term leverage. Items 1–3 interlock: pure-logic tests (2) can start immediately, but the deeper test coverage everyone wants requires the decoupling in (1), and the safest way to do (1) and (4) is behind tests. Recommended sequence: **2 (phase 1) → 1 → 3 → 2 (phase 2) → 4 → 5**.

---

### Recommendation 1 — Break the `SoundManager` singleton coupling with an injected audio context

- **Problem:** 51 direct static references to `SoundManager` across 16 runtime files, including from the data layer (`AudioEntity`), value types (`PlaybackPreference` constructor), and `SoundID` extensions. The `IAudioMixerPool` abstraction exists but is re-bound to `SoundManager.Instance` at use sites.
- **Impact:** This is the root enabler. Decoupling makes the runtime unit-testable (prerequisite for Rec. 2 phase 2), removes an entire class of teardown-ordering bugs currently handled by scattered null-guards, restores correct layering (data no longer depends on the manager), and opens the door to multiple audio contexts and editor-preview reuse of runtime code.
- **Recommendation:** Introduce a small internal context interface set — e.g. `IAudioContext` aggregating what already exists piecemeal: `IAudioMixerPool` (tracks/players), a settings provider (`RuntimeSetting` access, fade eases, warm-up time), and a playback registry (`TryGetAudioTypePref`, comb-filter tracking). `SoundManager` becomes *an implementation* of it. The static `BroAudio` facade keeps its exact public API and simply resolves the default context.
- **Implementation approach:**
  1. Define the interfaces next to `IAudioMixerPool`; make `SoundManager` implement them (no behavior change).
  2. `AudioPlayerObjectPool` stamps the context into each `AudioPlayer` on creation; replace `Mixer => SoundManager.Instance` and the direct `Instance.*` calls in `AudioPlayer.*` partials with the injected reference.
  3. Move default-ease resolution out of `PlaybackPreference`'s constructor: resolve defaults at play time in `SoundManager.Play` (which already has the context) and pass them in, so the struct becomes constructible anywhere.
  4. Purge `SoundManager` references from `Ami.BroAudio.Data`: `AudioEntity.HasLoop()` already has an overload taking explicit chained-loop defaults — delete the parameterless-default overload's singleton reach and make runtime callers supply values from the context.
  5. Leave `SoundID` convenience extensions delegating to the facade (they are user-facing sugar), but route them through `BroAudio.Manager` semantics consistently.
- **Priority:** **High**
- **Trade-offs:** Broad mechanical churn across many partials; several `public` members move or change signature (most consumers use the facade, so real user-facing breakage should be near zero, but audit the public surface). Teardown paths are delicate — migrate one subsystem at a time and keep the existing null-safe guards until each path is covered by a test. Do not attempt this in one PR.

---

### Recommendation 2 — Stand up an automated test suite and CI gate

- **Problem:** ~31.7k lines of timing-critical middleware with zero automated tests, no test asmdef, and no CI job that compiles the package in any configuration. Recent history is a sequence of hand-found regressions in pause/resume, teardown, and pitch/schedule interactions.
- **Impact:** Tests convert every other recommendation from "risky rewrite" to "safe refactor." A compile-matrix CI job alone would catch the most common failure mode of this package style (breaks when Addressables/Localization is absent, or on WebGL). Regression tests over schedule math directly protect the code that historically breaks.
- **Recommendation:** Unity Test Framework suite under `Assets/Tests/` (EditMode + PlayMode asmdefs, `InternalsVisibleTo` from the runtime asmdef), plus a CI workflow (e.g. game-ci) running EditMode tests and matrix compile checks on every push to `DEV_Unity6`.
- **Implementation approach:**
  1. **Phase 1 (immediately, no refactor needed):** EditMode tests for pure logic — clip-selection strategies (sequence/shuffle/velocity/chained), `FlagsExtension`, `FadeData` override-consumption semantics, `BroAudioType` iteration, volume/decibel conversion in `AudioExtension`.
  2. **CI:** batchmode compile of the package for: default, no-Addressables/no-Localization (fresh manifest), WebGL build target, and `BroAudio_InitManually`. Run EditMode tests in the same job. Wire into `merge-dev-to-main` as a required check.
  3. **Phase 2 (after Rec. 1/3):** unit tests for schedule math (`ResolveScheduledTiming` equivalents, pause rebase, pitch-adjusted end, seam start) as pure functions; PlayMode smoke tests booting the real `SoundManager` prefab: play → end recycles, pause → resume timing, seamless-loop handover preserves callbacks/decorators, BGM transition, stop-during-handover.
- **Priority:** **High**
- **Trade-offs:** CI needs a Unity license activation and ~minutes-long jobs; PlayMode audio tests on headless runners have no audio device (DSP clock still advances in batchmode, but treat audibility as untestable — assert on state/timing, not sound). Ongoing maintenance cost of the suite; flaky-test risk for timing assertions (use generous tolerances tied to DSP buffer size, not wall-clock).

---

### Recommendation 3 — Extract the playback state machine from the `PlayControl` coroutine into an explicit, pure scheduling model

- **Problem:** `PlayControl()` is a ~160-line coroutine interleaving resume, scheduling, BGM transition, fades, handover, and end detection; the DSP-timing arithmetic is smeared across `ResolveScheduledTiming`, `RebaseScheduleAfterPause`, `RecalculateScheduledEndTime`, and `ScheduleNextPlayback`, and is only executable inside Play Mode with real time passing. This is where the recent bugs cluster.
- **Impact:** The buggiest, most delicate logic becomes unit-testable arithmetic; the coroutine shrinks to orchestration; future features (new loop modes, transition types) get a defined state model to extend instead of a flag soup (`isResuming`, `isMusicPlayer`, `ChainedModeStage`, `_stopMode`, `IsStopping`, `HasStartedPlaying`).
- **Recommendation:** Two-step altitude change, no big rewrite:
  1. **Extract the math.** A `PlaybackSchedule` value type owning `startDspTime`, `endDspTime`, `pauseDspTime`, `isEndDerivedFromClip`, with pure static functions: `Resolve(now, warmUp, pitch, duration, pref)`, `RebaseAfterPause(now)`, `SeamStart(end, seamFade, warmUp)`, `RecomputeEndForPitch(...)`. The coroutine calls these; tests exercise them directly with synthetic DSP times.
  2. **Name the states.** An internal enum (`Idle → AwaitingLoad → Scheduled → Playing → FadingOut → Paused → Ended`) with transitions asserted in one place, replacing the boolean-flag combinations. Keep coroutines as the *driver*; a later move to a centrally ticked update (one `SoundManager` update iterating active players, mirroring the existing `LateUpdate` playback queue) becomes optional and mechanical, and would also remove per-play coroutine allocations.
- **Priority:** **High** (step 1), Medium (step 2)
- **Trade-offs:** Touches the most fragile code in the project — sequence it after Phase-1 tests and land PlayMode smoke tests alongside. Behavior must be preserved to the DSP tick; the empirical engine facts (warm-up floor, `isPlaying` semantics, `timeSamples` rest position) must carry over into the new model's assumptions or the same bugs will be re-fought. Step 2's central ticking changes callback ordering subtly (`_onUpdate` timing) — verify against documented user-facing behavior.

---

### Recommendation 4 — Replace the implicit handover transfer protocol with an explicit playback session object

- **Problem:** Surviving a seamless-loop seam requires manual, per-feature state transfer: `PlaybackHandoverData`'s fields, five `Transfer*` methods invoked by `AudioPlayerInstanceWrapper.UpdateInstance`, and `MusicPlayer` patching its static `_currentBGMPlayer`. Any new stateful `AudioPlayer` feature that doesn't opt in silently loses state at the first seam — an unguarded extension point in the package's signature feature.
- **Impact:** Turns "seamless loop keeps working when we add features" from tribal knowledge into a structural guarantee; deletes the most intricate glue code (`UpdateInstance`'s transfer choreography); makes the wrapper nearly trivial.
- **Recommendation:** Invert ownership. Introduce a pooled `PlaybackSession` object that owns everything logically belonging to *the playback* rather than *the player component*: user callbacks (`onStart/onUpdate/onPause/onEnd`), decorator list, added-effect descriptors, `PlaybackPreference` continuity, and the wrapper link. `AudioPlayer` becomes a stateless-ish executor holding a reference to its current session; handover reassigns `session.CurrentPlayer` — no per-field transfer, no delegate `GetInvocationList()` copying, and `MusicPlayer` tracks the session (stable identity) instead of the player instance.
- **Implementation approach:**
  1. Create `PlaybackSession` (pooled alongside players); move the four event fields and `_decorators`/`_addedEffects` into it; `AudioPlayer` members forward to the session.
  2. Fold `AudioPlayerInstanceWrapper` into (or over) the session — it already is the user-facing stable identity; `UpdateInstance` reduces to swapping the player pointer and re-binding the effect components (the only genuinely per-GameObject state).
  3. Re-point `MusicPlayer._currentBGMPlayer` to hold the session; delete the backing-field patching in its `UpdateInstance`.
  4. `PlaybackHandoverData` shrinks to the audio-thread facts (clip, schedule, pitch, track state).
- **Priority:** **Medium** (High value, but sequence after Recs. 1–3 provide test cover)
- **Trade-offs:** This redesigns object lifetime in the core loop — session pooling/reset bugs would manifest as cross-playback state bleed, which is worse than today's state loss; requires disciplined `Reset()` and tests for recycle paths. Decorator API surface moves; `internal` churn is significant even if the public API is unchanged.

---

### Recommendation 5 — Quarantine legacy migration debt and put the compile/config matrix under CI

- **Problem:** Legacy conversion executes inside hot paths (`SoundID._fixLegacyId` on every `Entity` access; obsolete serialized fields inside `SoundManager`, `AudioEntity`); the runtime assembly reflects into the editor assembly by type-name string; 86 runtime `#if`s across five dimensions with an inline WebGL duplicate of the master-volume fade; no automated verification that any non-default configuration compiles.
- **Impact:** Core types stop paying a permanent tax for one-time upgrades; the reflection landmine (silent breakage on a rename) is removed; WebGL stops being a parallel implementation to keep in sync; configuration breakage surfaces in CI instead of user bug reports. Collectively this is the "keep the codebase from silently rotting" item.
- **Implementation approach:**
  1. Move legacy-ID conversion into the existing editor upgrade tooling (`SoundIDUpgrader` / `FileStructureUpgrader`) as a versioned, run-once migration on asset import; after migration, `SoundID.Entity` becomes a plain field read. Keep a build-blocking editor validation ("project contains unmigrated SoundIDs") instead of runtime self-healing.
  2. Replace `Type.GetType("Ami.BroAudio.Editor.BroEditorUtility, BroAudioEditor")` with an editor-installed delegate: a `static Func<int, AudioEntity>` hook on a runtime class, assigned by an `[InitializeOnLoad]` editor initializer — compiler-visible on both sides.
  3. Unify WebGL master volume behind a tiny output abstraction (mixer-parameter writer vs. linear-volume writer) so one fade loop serves both; keep the `#if` at the abstraction's construction only.
  4. Schedule deletion milestones for `[Obsolete]` members (`BroAudioData`, `SoundManager._data`, `AudioEntity.ID`) tied to a major version, with the Asset Store upgrade path documented in `Docs/RELEASE_NOTES.md`.
  5. The CI matrix from Rec. 2 enforces all of it continuously.
- **Priority:** **Medium**
- **Trade-offs:** Upgrade-path changes are the riskiest kind for an Asset Store package — users jump multiple versions at once, so the run-once migration must handle very old data and must not destroy unmigrated references when it fails (keep it transactional: validate, then write). Removing runtime self-healing means a user who bypasses the editor migration gets a hard error instead of silent conversion — arguably better, but it changes support characteristics. WebGL unification must be verified on an actual WebGL build (audio there has enough platform quirks that "compiles" ≠ "works").

---

## 4. Not in the Top Five (worth tracking)

- **Editor window decomposition.** `LibraryManagerWindow`, `AudioEntityEditor`, `ReorderableClips` would benefit from splitting view code from asset-mutation code (the mutation halves are what you'd want to reuse/test). Do it opportunistically when features touch those files; a dedicated refactor isn't the best use of effort while runtime items 1–4 are open.
- **Pool-iteration invariants.** Make `GetCurrentAudioPlayers()` return a snapshot or document/assert the "no synchronous recycle during forward iteration" invariant; today it's implicit and one semantics change away from a runtime exception.
- **Per-play allocations.** Pool `AudioPlayerInstanceWrapper` (or fold into the Rec. 4 session), avoid `GetInvocationList()` on handover (also solved by Rec. 4), replace the `_addedEffects.Any(...)` LINQ in `AddAudioEffect` with a loop. Low urgency — amortized cost is small — but middleware is judged on GC pressure.
- **Mixer-asset/code contract validation.** An editor test asserting that every track/parameter name in `BroName` exists in `BroAudioMixer` would catch the "engine crashes on null parameter name" class of failure at import time instead of at runtime.
- **Consolidate fading systems.** `EffectAutomationHelper`'s waitable/tweaker hierarchy and `Fader` are two parallel animation systems; after Rec. 3's schedule extraction, a single tween utility could serve both.

---

*Review conducted July 2026 against branch `DEV_Unity6` (v3.2.2).*
