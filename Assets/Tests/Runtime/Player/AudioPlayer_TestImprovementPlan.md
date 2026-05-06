# AudioPlayer Test Improvement Plan

Scope reviewed:

- `Assets/BroAudio/Runtime/Player/AudioPlayer.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Volume.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Pitch.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Scheduling.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Recycling.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayerInstanceWrapper.cs`
- `Assets/Tests/Runtime/Player/AudioPlayerCharacterizationTests.cs`

The existing suite is a solid characterization layer for guard conditions, public-property defaults, and "happy path with zero-length fade" integration paths. The biggest gap is that almost none of the playback *time-evolution* paths (loop handover, fade-in, fade-out, scheduled end, pause/resume) are exercised. Several characterization tests are also slightly stale or rely on weak assertions that should be tightened.

---

## 1. High-priority gaps — uncovered behaviors

These represent real product behavior with no tests today. They are listed roughly in order of risk.

### 1.1 Loop handover (largest gap)

`PlayControl`, `ScheduleNextPlayback`, `BeginHandover`, `ReceiveHandover`, and `CanHandover` form the loop / chained-mode pipeline. This is the most complex code in `AudioPlayer.Playback.cs` and is entirely uncovered.

Suggested tests:

- **`Loop_OnEndReached_RequestNextPlayerInvokedWithExpectedPref`** — set `LoopType.Loop`, capture the `PlaybackHandoverData` passed to `RequestNextPlayer`, and assert `Pref.ScheduledStartTime/ScheduledEndTime`, `Pref.ChainedModeStage = Loop`, `TrackEffect`, `TrackVolume`, `Pitch`.
- **`SeamlessLoop_HandoverHappensBeforeFadeOut`** — verify `BeginHandover` is called *before* the fade-out completes (lines 200-203). This protects the fade overlap behavior that the SeamlessLoop variant relies on.
- **`Loop_ScheduleNextPlayback_AppliesFadeOffsetForSeamlessLoop`** — assert `newPref.ScheduledStartTime -= fadeOut` only happens for SeamlessLoop and only when `!isEnd`.
- **`ChainedMode_AtPlaybackStageStart_RequestsNewClip`** — `needNewClip` is true → `handover.Clip == null` and `newPref.ScheduledEndTime == 0` (recalculation reset on lines 271-274).
- **`ChainedMode_AtPlaybackStageEnd_PassesIsEndTrue`** — ensure `Stop()` while playing a chained-mode clip schedules the end-stage handover (`ScheduleNextPlayback(..., isEnd: true)` from `StopControl`).
- **`ReceiveHandover_RestoresTrackVolumePitchAndEffect`** — call `ReceiveHandover` directly, assert `_trackVolume.Target`, `StaticPitch`, and `CurrentActiveTrackEffects` mirror the input handover.
- **`BeginHandover_SwapsInstanceWrapperAndClearsNextPlayer`** — instance-wrapper swap to the new player, original `_nextPlayer` and `_instanceWrapper` cleared.
- **`Stop_DuringScheduledHandover_StopsNextPlayer`** — line 293 (`_nextPlayer?.Stop(FadeData.Immediate, …)`) is reached when a second `ScheduleNextPlayback` runs.

### 1.2 Fade-in / fade-out time evolution

Existing playback tests use `FadeData.Immediate (0f)`, which skips the actual fade branches in `PlayControl` (lines 171-179, 191-212). The `_clipVolume` ramp is therefore never observed under test.

Suggested tests:

- **`Play_WithFadeIn_ClipVolumeRampsFromZeroToTarget`** — fade-in on a clip with non-zero `FadeIn`, sample `_clipVolume.Current` mid-fade, assert it ends at `_clip.Volume * masterVolume`.
- **`Play_WithFadeOut_BeginsBeforeEndDspTime`** — verify the `while (AudioSettings.dspTime < fadeOutDspTime)` wait and that `_clipVolume.SetTarget(0f)` is invoked.
- **`StopControl_AlreadyFadingOut_WaitsForEndSample`** — covers lines 407-421 (the `_clipVolume.IsFadingOut` branch where StopControl waits for the AudioSource to reach `endSample` instead of starting a new fade).
- **`StopControl_WithCustomFadeOut_FadesClipVolumeToZero`** — non-immediate fade with `StopMode.Stop`; assert `_clipVolume` reaches 0 before `EndPlaying` runs.

### 1.3 Pause / Resume round trip

The current suite covers:

- Pause when not playing (synchronous `_onPaused`)
- Pause when playing with immediate fade
- UnPause when not paused (warning)

It does **not** cover the actual resume path from a real paused state.

Suggested tests:

- **`UnPause_AfterPause_ResumesPlayback`** — Play → Pause(Immediate) → UnPause → assert `IsPlaying` returns true, no warning logged, `_pref.HasFadeIn(...)` honored if set.
- **`UnPause_WithCustomFadeIn_AppliesFadeInOnNextPlayInternal`** — `_pref.SetNextFadeIn(fadeIn)` is the side effect; assert `_clipVolume` ramps after UnPause.
- **`Pause_WhenPlayingWithFadeOut_FadesBeforePausing`** — verify `_clipVolume.SetTarget(0f)` + Fade is awaited *before* `AudioSource.Pause()` is called.
- **`StopMode.Mute_FollowedByPlay_RecoversVolume`** — Mute zeroes `_trackVolume`; calling `Play` again should restore.

### 1.4 SetTrackEffect — full state machine

The existing suite tests:

- Guards (invalid ID, None+Add, None+Override+no-mixer)
- Add Volume → bitmask + mixer channel switch (one test against `BroAudioMixer`)

Missing transitions:

- **`SetTrackEffect_RemoveExistingBit_FlipsToDirectChannel`** — Add Volume, then Remove Volume → effect param muted, direct param restored.
- **`SetTrackEffect_OverrideToNone_ClearsBitsAndMutesEffectChannel`** — Add Volume, Override(None) → `IsUsingTrackEffect` flips back to false, mixer channel switches.
- **`SetTrackEffect_AddSecondEffect_DoesNotChangeChannel`** — already on effect channel; adding another bit should *not* re-call `ChangeChannel` (currently it doesn't because `oldUsingEffectState == newUsingEffectState`, but no test pins this).
- **`ResetEffect_WhenUsingTrackEffect_MutesSendChannel`** — directly invoke `ResetEffect` after Add and verify `mixer.SafeSetFloat(GetSendParaName(), MinDecibel)` happened.
- **`GetSendParaName_LazilyCachesTrackName`** — characterize the lazy cache + reset on `AudioTrack` setter (the `_currTrackName`/`_sendParaName` reset to null on null mixer assignment).

### 1.5 Volume — mixer-decibel write path

Section 13 only tests the `!HasStartedPlaying` early return. The actual `UpdateVolume` write path (lines 75-79 of Volume.cs) is uncovered.

Suggested tests:

- **`UpdateVolume_WhenStartedAndMixerAvailable_WritesDecibelToMixer`** — set up a real `BroAudioMixer` track, set `_trackVolume`/`_clipVolume`/`_audioTypeVolume`, force `HasStartedPlaying`, call `UpdateVolume`, read back the mixer parameter.
- **`UpdateVolume_WhenStartedAndMixerUnavailable_FallsBackToAudioSourceVolume`** — no `outputAudioMixerGroup` → `TrySetMixerDecibelVolume` returns false → `AudioSource.volume` is written instead.
- **`GetVolume_ReturnsProductOfAllThreeFaders`** — `IAudioPlayer.GetVolume()` returns `clip * track * audioType` (currently zero by default because clip volume defaults to 0).
- **`SetVolume_FromHigherToLower_UsesFadeOutEase`** vs **`FromLowerToHigher_UsesFadeInEase`** — `SetVolumeInternal` chooses ease based on direction; nothing pins this.

### 1.6 Scheduling — coroutine evolution

Section 20 covers the *entry* into scheduling but not the time-evolution side of it.

Suggested tests:

- **`WaitForScheduledStartTime_DecrementsSecondsUntilStart`** — yield several frames, assert `_secondsUntilScheduledStart` decreases monotonically by `Utility.GetDeltaTime()`.
- **`SchedulePlayback_WithScheduledEndTime_CallsAudioSourceSetScheduledEndTime`** — assert `AudioSource.SetScheduledEndTime` is honored when `_pref.ScheduledEndTime > 0`.
- **`CheckScheduledEnd_WhenAudioSourceStops_TriggersEndPlayingAndUnsubscribes`** — drive the AudioSource to `!isPlaying` after a scheduled end → `EndPlaying` fires, `_onUpdate` no longer contains `CheckScheduledEnd`.
- **`SetScheduledStartTime_WhenAlreadyPlaying_AdjustsAudioSourceSchedule`** — covers lines 37-41 of Scheduling.cs (re-scheduling while playing pauses until dspTime).
- **`SetScheduledStartTime_WhenAlreadyScheduled_AccumulatesIntoSecondsUntilStart`** — line 32 (`_secondsUntilScheduledStart += dspTime - _pref.ScheduledStartTime`).

### 1.7 InstanceWrapper.UpdateInstance — transfer pipeline

Section 15 verifies that `UpdateInstance` swaps the underlying ID, but the *transfer* of events, decorators, and effect components is the whole point of the wrapper. None of that is tested.

Suggested tests:

- **`UpdateInstance_TransfersOnUpdateOnEndOnPauseToNewPlayer`** — register handlers on player A, call `UpdateInstance(B)`, fire each event on B, assert handlers fire on B and not on A.
- **`UpdateInstance_TransfersDecoratorsAndCallsUpdateInstanceOnEach`** — set up a `MusicPlayer` decorator on A → after `UpdateInstance(B)`, the decorator's `Instance` points to B and `B.IsBGM == true`.
- **`UpdateInstance_TransfersAddedEffectComponents_CopiesValues`** — directly tests `TransferAddedEffectComponents` / `SetAddedEffectComponents`: add a LowPass effect with a non-default `cutoffFrequency`, transfer to B, verify B has the component with the same configured value.
- **`UpdateInstance_WhenSourceUnavailable_BypassesTransfer`** — `IsAvailable(false)` is false → `base.UpdateInstance` runs without TransferOn* calls (covers line 112-115).

### 1.8 Effect-component lifecycle — beyond the guard

- **`AddAudioEffect_InvokesOnSetCallbackWithProxy`** — `onSet?.Invoke(modifier as TProxy)` (line 285) — the proxy passed in must be non-null and the callback must run.
- **`Recycle_DestroysAllAddedEffectComponents`** — `DestroyAddedEffectComponents` removes `LowPass` etc. from the GameObject after `Recycle()`.
- **`OnAudioFilterRead_AddsAudioFilterReaderComponent`** — verify `_audioFilterReader` exists and its `OnTriggerAudioFilterRead` matches the supplied delegate.
- **`Recycle_DestroysAudioFilterReader`** — pair to the above; `_audioFilterReader` is destroyed in `Recycle`.

### 1.9 PlayControl — error / edge paths

- **`Play_WhenHasStartedPlayingTrue_LogsCleanupErrorAndEndsPlaying`** — force `PlaybackStartingTime > 0` (via reflection or via a successful prior `Play`) and call `PlayInternal` again. Currently undocumented.
- **`Play_WhenAudioTypePrefMissing_LogsErrorAndEndsPlaying`** — covers the second `ValidatePlayback` in `PlayInternal`.
- **`Play_WhenPickNewClipReturnsNull_EndsPlayingWithoutError`** — `_clip ??= _pref.PickNewClip()` returns null → `EndPlaying` without exception.
- **`Play_WhenAudioClipIsNull_LogsErrorAndEndsPlaying`** — `audioClip == null` validation (lines 97-102).
- **`Play_CoroutineThrows_CatchesExceptionAndEndsPlaying`** — covers the try/catch around `RestartCoroutine` (lines 60-68).

### 1.10 MusicPlayer transition inside PlayControl

Lines 146-155 wait for `musicPlayer.IsWaitingForTransition`. Untested.

- **`Play_WhenAsBGM_OverridesReverbAndPriorityAndAwaitsTransition`** — register `AsBGM` decorator with a transition, observe that `AudioSource.priority = AudioConstant.HighestPriority`, `reverbZoneMix = 0`, and `Play` doesn't call `StartPlaying` until the transition reports complete.

### 1.11 Dominator / Track-type wiring

- **`Play_WhenAsDominator_SetsTrackTypeToDominatorAndSkipsAddEffect`** — covers lines 115-122 of Playback.cs (`if (IsDominator) TrackType = Dominator; else SetTrackEffect(...)`).

### 1.12 Recycle path coverage

`Recycle` is partially covered (Section 25). Missing:

- **`Recycle_StopsPlaybackControlAndHandoverCoroutines`** — start `Play`, capture coroutine fields, call `Recycle`, assert both coroutine fields are null and the coroutines no longer drive the AudioSource.
- **`Recycle_ReturnsTrackToMixerPool_WhenAudioMixerGroupAssigned`** — the `TryGetMixerAndTrack` branch on lines 33-37 (`MixerPool.ReturnTrack`).
- **`Recycle_ResetsTrackTypeToGeneric_WhenWasDominator`** — pair with the Dominator test.
- **`Recycle_DisposesAudioSourceProxy`** — `_proxy?.Dispose()` in `ResetAudioSource`.

### 1.13 Pitch — additional behaviors

- **`SetPitch_WhenAudioMixerSetting_DoesNotWriteAudioSourcePitch`** — switch `SoundManager.PitchSetting` to `AudioMixer`; `AudioSource.pitch` should remain at 1.0 even after `SetPitch(2f)`. Documents the dead-code-ish branch.
- **`SetPitch_AboveMaxAudioSourcePitch_ClampsToMax`** / **`BelowMin_ClampsToMin`** — line 27.
- **`SetInitialPitch_UsesStaticPitchWhenNonDefault`** vs **`UsesAudioTypePitchWhenStaticIsDefault`** vs **`FallsBackToEntityGetPitch`** — covers all three branches of the priority chain.
- **`ResetPitch_RestoresDefaults`** — assert `StaticPitch == DefaultPitch` and `AudioSource.pitch == DefaultPitch` after `EndPlaying`.

### 1.14 Spatial — uncovered branches

Section 24 covers follow-target and `ResetSpatial`, but not:

- **`SetSpatial_WithSpecifiedPosition_PositionsTransformAndForces3D`** — `pref.HasSpecifiedPosition(out var position)` branch (line 140).
- **`SetSpatial_With2DSpatialBlendCurve_DoesNotForce3D`** — when the entity's curve `IsDefaultCurve(SpatialBlend_2D)`, line 162 forces 3D anyway because a position was given. Pin this surprising behavior.
- **`SetSpatial_WithCustomRolloff_AppliesCurve`** — line 122-125.
- **`SetSpatial_WithLowPassFilter_AddsLowPassEffectOnce`** — `_addedEffects == null` guard (lines 127-131); ensure a transferred player from handover does *not* re-add the filter.

### 1.15 InstanceWrapper — fluent return + Empty fallbacks

Existing tests cover `SetVolume` and `SetFadeInEase` after recycle. Other interface methods take the same shape but aren't pinned:

- **Parameterized test across `SetPitch / SetVelocity / OnEnd / OnStart / OnUpdate / OnPause / SetFadeOutEase / SetScheduledStartTime / SetScheduledEndTime / SetDelay / AddAudioEffect / RemoveAudioEffect / OnAudioFilterRead`** — after `Recycle`, each must return `Empty.AudioPlayer`. Currently a single test stands in for all of them.
- **`InstanceWrapper_AudioSource_WhenInstanceNull_ReturnsNull`** — line 67 of `AudioPlayerInstanceWrapper.cs` (returns `null`, not `Empty.AudioSource`). Worth documenting because it's inconsistent with the inactive-Instance behavior.
- **`InstanceWrapper_LogInstanceIsNull_RespectsLogAccessRecycledPlayerWarningSetting`** — toggle the setting; the warning must / must not fire.
- **`InstanceWrapper_GetDecorator_OnDecoratorMethodAfterRecycle_DoesNotThrow`** — `IMusicPlayer.SetTransition` etc. when `Instance` is null — `GetDecorator` dereferences `Instance` directly (line 178). This is actually a latent NRE; either the test pins current behavior or surfaces a bug worth flagging.

---

## 2. Tests to remove, rename, or strengthen

### 2.1 Rename / fix stale labels

- **`OnPlaybackHandover_OnFreshPlayer_IsNull`** (Section 1, line 318) — the field is now `RequestNextPlayer`; rename to `RequestNextPlayer_OnFreshPlayer_IsNull`.
- **`Stop_StopModeWhilePlaying_FadesAndCallsEndPlaying`** (Section 17) — uses `0f` fade time, so no fade is exercised. Either rename to `Stop_WithImmediateFade_EndsPlaying` or replace with a real fade-time variant (see 1.2).
- **`SchedulePlayback_WithClipDelay_SetssecondsUntilScheduledStart`** — typo (`SetssecondsUntil…`); rename to `SetsSecondsUntilScheduledStart`.

### 2.2 Tighten weak assertions

- **`AudioSourceProperty_AccessedTwiceWhileInactive_ReturnsSameEmptyInstance`** (line 855) — uses `LogAssert.Expect(Error, ".*")` twice. The match pattern accepts any error and hides regressions. Replace with the actual error regex (`"audio player is not playing"`).
- The proxy `??=` lazy-init claim in the comment is misleading — when inactive the method returns `Empty.AudioSource` directly without ever touching `_proxy`. Either rewrite the test to actually exercise the lazy init (when active) or simplify the comment to "both calls return the shared Empty.AudioSource singleton".

### 2.3 Reduce redundancy

- **Section 7** — six near-identical "returns this" tests. Acceptable but could be folded into a single `TestCaseSource` if test count starts to bloat. Low priority.
- **Section 22** — `AddAudioEffect_WhenSameTypeAlreadyExists_LogsWarning`, `RemoveAudioEffect_WhenEffectExists_RemovesFromList`, and `AddThenRemove_RoundTrip_CleansUpList` — the last is fully implied by the first two. Drop `AddThenRemove_RoundTrip_CleansUpList` or repurpose it to a *behavior* assertion (e.g., the destroyed Component is no longer attached to the GameObject).
- **Section 23** — `SetVolume_ZeroFadeTime_CompletesImmediately` and `SetAudioTypeVolume_StoresValueInFader` exercise the same `SetVolumeInternal` zero-fade branch. Keep one, or merge into a parameterized test that runs over all three Faders.

### 2.4 Brittle reflection / global state

- **Mutating `BroAudioMixer` parameters in `SetTrackEffect_AddVolume_…`** (Section 17): the test relies on `Track3` being defined in the shared mixer asset and writes to it. If the test fails before the restore step, mixer state leaks across tests.
  - Mitigation: wrap the restore in `try/finally`, or save the original parameter values at the top and restore them regardless of pass/fail, or build a dedicated test mixer asset and load it instead of `BroAudioMixer`.
- **`SetAutoProperty(_player, "IsStopping", true)`** (used in 18 & 19): writes to a `private set` auto-property's compiler-generated backing field. If the property is renamed, refactored to a manual field, or expression-bodied, the helper silently breaks. Add a smarter helper that falls back to a real setter (`(player as IAudioStoppable).Stop` could be used in some cases) or a more descriptive failure message. Adding an `internal` test seam (e.g., an `internal void SetIsStoppingForTests(bool value)` behind `INTERNALS_VISIBLE_TO`) would be cleaner long-term.
- **Resetting `SoundManager._instance` via reflection** (`TearDownStubSoundManager`): fine for now, but if tests run in parallel within the same play-mode session this becomes a race. Consider documenting "must run sequentially" or use `[NonParallelizable]`.

### 2.5 Suspicious assertions worth re-reviewing

- **`Stop_PauseModeWhenNotPlaying_SetsStopModeAndFiresOnPausedSynchronously`** asserts `IsStopping == false`. That's correct for the early-return path, but the comment "no coroutine started" should also be backed up by checking that `_playbackControlCoroutine == null` to be defensive against future refactors.
- **`SetPlaybackData_WithValidEntity_SetsIDAndMakesIsActiveTrue`** — `IsActive` only checks ID validity. The test is correct as a characterization, but a reader might infer "IsActive means audible". Add a comment explicitly stating that `IsActive` is derived from `ID.IsValid()` and not from `AudioSource.isPlaying` (the existing `IsActive_ReflectsOnlyWhetherIDEntityIsNonNull` already does this — link the two with a `<see cref="…" />`).

---

## 3. Suggested structural improvements

These are not new tests but make the suite easier to extend.

1. **Introduce a `PlayerTestFixture` base class** with `SetupStubSoundManager`, the reflection helpers, `MakeEntity*`, `NullMixerPool`, and entity cleanup. The current 2,000-line file is hard to navigate.
2. **Split by concern** — `AudioPlayerCharacterizationTests.Initial.cs`, `…Playback.cs`, `…Volume.cs`, `…Pitch.cs`, `…Scheduling.cs`, `…Recycling.cs`, `…InstanceWrapper.cs`. Each partial focuses on one source-side partial.
3. **Add `[InternalsVisibleTo("Ami.BroAudio.Tests")]`** to the BroAudio runtime assembly. Roughly half of the reflection in this file (Transfer*, _onEnd backing field, _addedEffects, _decorators, _onPaused, _onStart, _onUpdate, _stopMode, _pref, _trackVolume, _audioTypeVolume, _clipVolume, _secondsUntilScheduledStart, SetInstanceWrapper, GetInstanceWrapper, ResetVolume) would disappear and the tests would become refactor-resistant.
4. **Add deterministic dsp-time helpers** — many of the new tests above need to advance `AudioSettings.dspTime` predictably. A small `WaitForDspTime(seconds)` coroutine helper that yields until `AudioSettings.dspTime >= start + seconds` would make scheduling tests stable across machines.
5. **Distinguish "Characterization" from "Specification" tests.** As the public API stabilizes, tests like §1.7 (transfer pipeline) and §1.4 (effect state machine) should probably be promoted to specification tests (asserting *intended* behavior, not just current behavior) so a refactor that improves them isn't blocked by characterization comments.

---

## 4. Suggested rollout order

1. Tighten weak assertions (§2.2) and rename stale tests (§2.1) — a quick win, no new behavior added.
2. Add `InternalsVisibleTo` (§3.3) and migrate the existing reflection-based tests to direct calls.
3. Cover the **Loop / Handover** pipeline (§1.1) — the largest correctness risk and the area most likely to regress under future changes.
4. Cover **fade-in / fade-out** (§1.2) and **pause/resume** (§1.3) — these protect everyday playback flows.
5. Cover **SetTrackEffect transitions** (§1.4) and **mixer-decibel volume** (§1.5).
6. Cover **scheduling time evolution** (§1.6) and **InstanceWrapper transfer** (§1.7).
7. Fill the rest (§1.8 – §1.15) opportunistically.

---

## 5. Open questions for the author

These would help me make the right calls before writing the new tests. None block this plan, but answers will tighten priorities.

1. **Is the lazy `_proxy` init in `IAudioPlayer.AudioSource` (line 54) intended to be observable?** I.e., should two consecutive calls return the same `AudioSourceProxy` (live path) by design, or is that an implementation detail that may change?
2. **Is the `AudioMixer` branch of `SetPitch` deliberately a no-op today** (lines 22-25), or is that a TODO? Behavior of that branch affects what §1.13 should pin.
3. **Is `InstanceWrapper.AudioSource` returning `null` (instead of `Empty.AudioSource`) when the instance is null intentional?** §1.15 needs to know whether to characterize the current behavior or flag it as a bug.
4. **For `AsBGM` / `AsDominator`, is the "get-or-create returns same instance" contract a guarantee or an implementation detail?** Section 21 of the existing suite pins it as a guarantee — confirming this lets us test more safely.
5. **`StopMode.Mute` followed by `Play` — is the expected behavior to re-ramp `_trackVolume` back to 1?** The current code resets `_trackVolume` only via `ResetVolume()` in `EndPlaying`, so a Mute→Play sequence may leave volume at zero. Worth confirming so the test in §1.3 either pins the bug or pins the correct behavior.
6. **Should characterization tests block on `BroAudioMixer` (a shared production asset) or on a dedicated test mixer?** Affects §2.4.
