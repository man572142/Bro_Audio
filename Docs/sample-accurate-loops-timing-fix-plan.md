# Plan: Fix first→second loop iteration timing drift (sample-accurate loops)

## Summary

The sample-accurate loop feature (merged in `733da2d`) has a timing glitch on the
**first → second** loop iteration: the second iteration starts slightly *earlier*
than the actual end of the first iteration's audio content (audible as an early
restart / tiny overlap). Every later iteration is correct.

The root cause is a trade-off baked into commit `6704e80`
("Remove warmUpTime for first loop iteration"). That commit removed the scheduling
lead time (`warmUpTime`) from the **first** iteration to avoid a volume race, but in
doing so it made the first iteration's playback start *unschedulable* at the exact
DSP time it claims to start — so the boundary it hands the second iteration is
computed from a start time the audio engine cannot actually honor.

This plan restores sample accuracy on the first boundary **and** keeps the volume
correct at the scheduled start, satisfying both of the user's bottom-line
constraints.

## User constraints (the bottom line)

1. **Fade-in timing may be slightly inaccurate**, BUT the *initial* clip volume must
   already be in place on the mixer **before** the scheduled audio produces its first
   sample:
   - `0` when there is a fade-in, or
   - `targetClipVolume` (`clip.Volume * Entity.GetMasterVolume()`) when there is no fade-in.
2. **The scheduled audio must be sample accurate** (the loop boundary handover must
   land on the exact DSP sample).

## Background: how the loop handover works today

Relevant files:
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Scheduling.cs`
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Volume.cs`
- `Assets/BroAudio/Runtime/SoundManager/SoundManager.cs`
- `Assets/BroAudio/Runtime/Extension/AudioConstant.cs`

Lifecycle of a looping entity:

1. The first player runs `PlayControl` (`AudioPlayer.Playback.cs:70`).
2. `ResolveEndDspTime` (`:270`) computes `startBaseTime` and `endDspTime`. For a loop
   with no prior schedule it stamps `_pref.ScheduledStartTime = startBaseTime` and
   `_pref.ScheduledEndTime = endDspTime` so the boundary is shared with the next player.
3. `SchedulePlayback` (`AudioPlayer.Scheduling.cs:14`) calls
   `AudioSource.PlayScheduled(max(ScheduledStartTime, dspTime))` and computes
   `_secondsUntilScheduledStart`.
4. As the player nears `endDspTime`, `ScheduleNextPlayback` (`:293`) sets the next
   player's `ScheduledStartTime = endDspTime` and, after waiting until
   `dspTime >= ScheduledStartTime - warmUpTime`, spawns the next player via
   `RequestNextPlayer` → `ReceiveHandover` → `PlayInternal`.
5. The next player schedules `PlayScheduled(endDspTime)` with `warmUpTime` of lead, so
   the audio engine *can* begin exactly at `endDspTime` → sample accurate.

`warmUpTime` (`SoundManager.ScheduledPlaybackWarmUpTime`, `SoundManager.cs:87`) is the
output/DSP-buffer latency (floored at `AudioConstant.MixerWarmUpTime = 0.1s`,
`AudioConstant.cs:78`). It is the lead time the audio engine needs in order to begin a
`PlayScheduled` clip *precisely* at the requested DSP time instead of "at the next
buffer boundary."

## Root cause

### Why the second iteration starts early

Commit `6704e80` changed the first iteration's `startBaseTime` from
`dspTime + warmUpTime` to just `dspTime` (`ResolveEndDspTime`,
`AudioPlayer.Playback.cs:279`). Consequences for the **first** player:

- `ScheduledStartTime = dspTime` (≈ "now"), so `SchedulePlayback` issues
  `PlayScheduled(max(dspTime, dspTime)) = PlayScheduled(now)`.
- The audio engine **cannot** start a clip exactly at the current DSP time — it begins
  at the next DSP buffer boundary, i.e. at `dspTime + δ`, where `δ` is up to one
  output-latency/buffer of delay.
- But `endDspTime = dspTime + duration` was computed from the *un-delayed* `dspTime`.
- The second iteration is scheduled at `endDspTime = dspTime + duration`, and is
  honored sample-accurately. Meanwhile the first iteration's actual audio content runs
  from `dspTime + δ` to `dspTime + δ + duration`.
- Net: the second iteration begins `δ` **before** the first iteration's audio
  actually finishes → the audible "starts earlier" glitch. `δ` ≈ the very
  `warmUpTime`/buffer latency that was removed.

Every subsequent boundary is fine because those players are scheduled into the future
with `warmUpTime` of lead (step 5), so their `PlayScheduled` is honored exactly.

### Why `warmUpTime` was removed in the first place (the volume race)

Restoring `warmUpTime` naively re-introduces the bug `6704e80` was fixing. The reason
is the ordering of volume setup vs. the scheduled start inside `PlayControl`, combined
with a guard in `UpdateVolume`:

- `UpdateVolume` (`AudioPlayer.Volume.cs:65`) **early-returns while
  `!HasStartedPlaying`**. So none of the `_clipVolume.Complete(...)` / `Fade(...)`
  calls actually push a level to the mixer until `HasStartedPlaying` becomes true.
- `HasStartedPlaying` (`PlaybackStartingTime > 0`) is only set at
  `AudioPlayer.Playback.cs:149`, which runs **after** `SchedulePlayback` (`:118`) and
  **after** `WaitForScheduledStartTime` (`:121`).
- The clip-volume setup (fade-in vs. `Complete(targetClipVolume)`) is at `:155–168`,
  i.e. *after* the wait as well.

So with a real `warmUpTime` lead, the sequence is: schedule → wait `warmUpTime` →
(audio engine begins at the scheduled time) → only *then* push the real volume. The
freshly acquired mixer track sits at `MinDecibelVolume` (it is silenced on return,
`AudioPlayer.VirtualTrack.cs:61`), so:

- **No fade-in:** the first buffer plays at the stale/silent track level instead of
  `targetClipVolume` — the clip's attack/transient is dropped. ← the race
  `6704e80` describes.
- **Fade-in:** the fade only begins after the audio is already running, so the very
  start can blip.

`6704e80` "fixed" this by collapsing `warmUpTime` to `0` so steps 3–6 all happen in the
**same frame** (no wait), pushing the volume before the engine processes the next
buffer — at the cost of the sample accuracy described above.

### The real conflict

- Sample accuracy on the first boundary **requires** scheduling the first start into
  the future with `warmUpTime` of lead.
- A future-scheduled start **requires** the initial clip volume to be present on the
  mixer **before** that scheduled time — which today is impossible because
  `UpdateVolume` no-ops until `HasStartedPlaying`, and `HasStartedPlaying` is set only
  after the wait.

The fix must do **both**: re-introduce the first-iteration lead time *and* establish
the initial volume on the mixer before the scheduled start.

## Proposed fix

### Part A — Restore sample accuracy for the first iteration

In `ResolveEndDspTime` (`AudioPlayer.Playback.cs:270`), reinstate the `warmUpTime`
lead for the **first** scheduled loop start (the same condition `6704e80` removed:
`HasLoop() && ScheduledStartTime <= 0`):

```
double warmUpTime = _pref.HasLoop() && _pref.ScheduledStartTime <= 0
    ? SoundManager.Instance.ScheduledPlaybackWarmUpTime
    : 0d;
double startBaseTime = _pref.ScheduledStartTime > 0
    ? _pref.ScheduledStartTime
    : AudioSettings.dspTime + warmUpTime;
```

Effect: the first player schedules `PlayScheduled(dspTime + warmUpTime)`, which the
engine *can* honor exactly. `endDspTime = startBaseTime + duration` now matches the
real content end, so the handover boundary the second iteration inherits is accurate.
This shifts the entire loop timeline later by `warmUpTime` (~≤100ms) once, at the very
first start, which is imperceptible and only affects loops — one-shots keep
`ScheduledStartTime <= 0` and still start immediately via `AudioSource.Play()`.

### Part B — Establish the initial clip volume before the scheduled start

Guarantee the mixer carries the correct *initial* level before the engine emits the
first scheduled sample. Restructure the volume handling in `PlayControl` so it happens
in two phases:

1. **Pre-start (before `WaitForScheduledStartTime`, right after `SchedulePlayback`):**
   - Compute `targetClipVolume = _clip.Volume * _pref.Entity.GetMasterVolume()` once.
   - Evaluate fade-in **once** and cache the result — note `HasFadeIn`
     (`PlaybackPreference.cs:75`) *consumes* a pending fade override
     (`TryGetOrConsumeOverride`), so it must not be called twice.
   - Set `_clipVolume` to its initial value and **force the push to the mixer now**,
     even though `HasStartedPlaying` is still false:
     - `0f` if there is a fade-in,
     - `targetClipVolume` if there is no fade-in.
2. **Post-start (after the start, where the existing `:155–168` block is):**
   - If there is a fade-in: `SetTarget(targetClipVolume)` then `Fade(...)` from the
     already-established `0`. The fade *start time* may drift by up to a frame — this
     is explicitly acceptable per the user's constraint — but it always starts from
     `0` because of phase 1.
   - If there is no fade-in: the level is already `targetClipVolume`; no action needed
     (or a redundant `Complete(targetClipVolume)` that is now a no-op).

#### The `UpdateVolume` guard

The blocker is that `UpdateVolume` (`AudioPlayer.Volume.cs:65`) returns early while
`!HasStartedPlaying`, so a normal `_clipVolume.Complete(value)` won't reach the mixer
during phase 1. Options (pick one during implementation; **B1 preferred**):

- **B1 (preferred): a dedicated pre-start push.** Add a small internal method on
  `AudioPlayer` (e.g. `EstablishScheduledStartVolume(float clipVol)`) that computes
  `clipVol * _trackVolume.Current * _audioTypeVolume.Current` and pushes it directly
  via the existing `TrySetMixerDecibelVolume(...)` / `AudioSource.volume` path used by
  `UpdateVolume`, bypassing the `HasStartedPlaying` guard. Call it only on the
  scheduled-start branch (`_pref.ScheduledStartTime > 0` and not resuming). This keeps
  the guard's intent (don't push during un-started immediate plays) intact while
  carving out the scheduled-start case where the source is already armed via
  `PlayScheduled` and the track is already acquired.
  - Also set `_clipVolume`'s internal `Current`/`Target` to the initial value so the
    later fade/`Complete` is consistent (use `Complete(value, updateBus: false)` to
    set state without going through the guarded `UpdateVolume`).

- **B2: relax the guard.** Allow `UpdateVolume` to push when the source is scheduled
  (`AudioSource.isPlaying` is already true after `PlayScheduled`) even if
  `HasStartedPlaying` is false. Lower-surface-area in `PlayControl` but changes a
  shared method used widely — higher regression risk; verify every caller.

> Note: after `PlayScheduled`, `AudioSource.isPlaying` already returns `true` (Unity
> reports scheduled sources as playing), and the mixer track is acquired in
> `SetupFreshSource`, so pushing the mixer level during phase 1 is safe and lands
> before the first audible sample.

### Resulting `PlayControl` ordering (target)

```
SetupFreshSource(...)              // _clipVolume.Complete(0f, false) as today
ResolveEndDspTime(...)             // Part A: first start = dspTime + warmUpTime
SchedulePlayback()                 // PlayScheduled(dspTime + warmUpTime)

// Phase 1 (Part B) — only on the scheduled-start branch:
targetClipVolume = clip.Volume * Entity.GetMasterVolume()
hasFadeIn = _pref.HasFadeIn(_clip.FadeIn, out fadeIn, out fadeInEase)  // call ONCE
EstablishScheduledStartVolume(hasFadeIn ? 0f : targetClipVolume)

if (_secondsUntilScheduledStart > 0) yield return WaitForScheduledStartTime()
... BGM transition / StartPlaying / PlaybackStartingTime / onStart ...

// Phase 2 (Part B): reuse cached hasFadeIn/fadeIn/fadeInEase — do NOT re-call HasFadeIn
if (hasFadeIn) { _clipVolume.SetTarget(targetClipVolume); _clipVolume.Fade(...); wait }
else           { /* already at targetClipVolume */ }
```

Keep the immediate (non-scheduled, `ScheduledStartTime <= 0`) path behaving exactly as
today: `StartPlaying()` runs in-frame and the existing post-start volume block already
pushes before the next buffer, so phase 1 should be a no-op there.

## Edge cases to verify

- **No fade-in, looping:** first buffer must be at `targetClipVolume` (no dropped
  attack); boundary to second iteration sample-accurate.
- **Fade-in, looping:** first buffer at `0`, fades up; fade may start ~a frame late
  (accepted). Boundary still accurate.
- **Seamless loop** (`ApplySeamlessFade`, fade-out subtracted from the next
  `ScheduledStartTime`, `:304`): confirm the first-iteration `warmUpTime` shift does
  not double-apply with the seamless fade offset.
- **Chained mode** (`PlaybackStage` Start/Loop/End): `needNewClip` resets
  `ScheduledEndTime`; confirm first-stage start still gets the lead time.
- **Pitch ≠ 1:** `PitchAdjusted` duration unaffected by the change.
- **One-shots / non-loop / explicit `SetDelay` / `SetScheduledStartTime`:** unchanged;
  they already pass `ScheduledStartTime > 0` or `<= 0` and should not gain/lose lead.
- **Resume after pause** (`isResuming`): phase-1 establishment must be skipped (it
  rebases an already-running source; volume is already live).
- **Addressables load wait** (`:84–93`): occurs before scheduling; no interaction.
- **WebGL** (`UpdateWebGLVolume`): mirror the pre-start push for the
  `#if UNITY_WEBGL` path so the same guarantee holds.

## Validation

No CLI audio pipeline exists; validate in the Unity Editor (Unity 6000.3):

1. Create a looping entity (a) with no fade-in, (b) with a fade-in. Use a clip with a
   sharp transient at sample 0 and a clear marker near the loop point.
2. Play and listen across the first→second boundary: the second iteration must no
   longer pre-empt the first; later boundaries unchanged.
3. Optional precise check: capture `AudioSettings.dspTime` at each handover and assert
   `iteration[n+1].start - iteration[n].start ≈ clipDuration` for n = 1 (the first
   boundary) within a sample, matching later boundaries.
4. Confirm the no-fade-in case has full level on the first sample (no soft attack) and
   the fade-in case starts from silence.
5. Verify one-shots and delayed (`SetDelay`) plays are unchanged.

## Definition of done

- First→second loop boundary is sample-accurate (matches all later boundaries).
- Initial clip volume is correct on the first scheduled sample: `0` with fade-in,
  `targetClipVolume` without.
- No compiler errors in changed files; Addressables/Localization untouched; WebGL path
  handled; one-shot/delay/pause behavior unchanged.
- `HasFadeIn` is evaluated exactly once per play (no double-consumed override).

## Files likely to change

- `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs` — `ResolveEndDspTime`
  (Part A) and `PlayControl` volume ordering (Part B).
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Volume.cs` — add the pre-start volume
  push helper (Part B, option B1), or adjust the `UpdateVolume` guard (option B2).
