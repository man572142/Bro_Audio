# Fix Plan: Mid-Play Pitch Change Breaks Scheduled End Times

## Problem

Since the sample-accurate loop work (merge `733da2d`), playback end is driven entirely by
absolute DSP times that are computed **once, at playback start**, using the pitch at that
moment. `IAudioPlayer.SetPitch()` changes `AudioSource.pitch` (instantly, or gradually via
`PitchControl`) but never updates any of those times. After a mid-play pitch change the
playhead reaches the clip's end position at a different wall-clock time than the schedule
expects, so:

- **Pitch raised**: the samples run out before `ScheduledEndTime` → trailing silence, and the
  next loop iteration (scheduled at the stale end time) starts late → audible gap at the seam.
- **Pitch lowered**: `AudioSource.SetScheduledEndTime()` hard-stops the source before the
  content reaches the clip's end position → audio cut off in the wrong place, and the next
  iteration starts from `StartPosition` even though the previous one never reached the seam →
  skipped audio.

The same staleness also shifts the fade-out start (`endDspTime - fadeOut`), the seamless-loop
handover moment, `EndPlaying()` for one-shots, and the pause/resume rebase if pitch is changed
while paused.

## Root cause — where the stale values live

All in `Runtime/Player/`:

| Location | Stale value |
|---|---|
| `AudioPlayer.Playback.cs` → `ResolveScheduledTiming()` | `endDspTime = start + PitchAdjusted(duration, pitch-at-start)`; written into `_pref.ScheduledEndTime` and `_playbackEndDspTime` |
| `AudioPlayer.Playback.cs` → `PlayControl()` | `endDspTime` captured as a **coroutine local**; used by the fade-out deadline and the `while (dspTime < endDspTime)` wait |
| `AudioPlayer.Playback.cs` → `ScheduleNextPlayback(double endDspTime)` | end time captured as a **parameter** at coroutine start; `newPref.ScheduledStartTime/EndTime` derived from it before the wait completes |
| `AudioPlayer.Scheduling.cs` → `ScheduleEndTime()` | arms the hardware stop via `AudioSource.SetScheduledEndTime(_pref.ScheduledEndTime)` — never re-armed |
| `AudioPlayer.Scheduling.cs` → `RebaseScheduleAfterPause()` | slides the stale end time by the pause duration; wrong if pitch changed while paused |
| `AudioPlayer.Pitch.cs` → `SetPitch` / `PitchControl` | changes `AudioSource.pitch` without notifying the scheduler |

A pre-spawned `_nextPlayer` (handover already requested inside the warm-up window) is also
stale: it was `PlayScheduled()` at the old seam time.

## Fix design

### Principle: recompute from the playhead, not from elapsed time

At any instant the correct remaining wall-clock time is derivable from state Unity already
tracks sample-accurately:

```
endSample        = audioClip.samples - GetSample(frequency, _clip.EndPosition)
remainingSeconds = (endSample - AudioSource.timeSamples) / frequency / currentPitch
newEndDspTime    = AudioSettings.dspTime + remainingSeconds
```

Because `AudioSource.timeSamples` reflects the *actual* progress regardless of pitch history,
this is correct after any number of pitch changes — including mid-fade — without integrating
pitch over time.

### Step 1 — central recompute method (`AudioPlayer.Scheduling.cs`)

Add `RecalculateScheduledEndTime()`:

1. Guard: no-op unless a clip is assigned, playback is active, and the player isn't paused
   (`_stopMode != StopMode.Pause`).
2. If the scheduled start is still in the future (inside the warm-up window,
   `_secondsUntilScheduledStart > 0`): `newEnd = _pref.ScheduledStartTime +
   PitchAdjusted(_clip.GetPlayableDuration(), AudioSource.pitch)`.
3. Otherwise use the playhead formula above.
4. Update `_playbackEndDspTime`; if `_pref.ScheduledEndTime > 0` **and it was derived from the
   clip duration** (see Step 4), update it and re-arm
   `AudioSource.SetScheduledEndTime(newEnd)` (re-arming on a playing source is supported).
5. If `_nextPlayer != null`, propagate the new seam: recompute its start
   (`newEnd`, minus the seamless fade-out if applicable) via the existing
   `ISchedulable.SetScheduledStartTime` / `SetScheduledEndTime` — these already handle the
   already-`PlayScheduled` case by calling `AudioSource.SetScheduledStartTime` on the source.

### Step 2 — make the wait loops read live state (`AudioPlayer.Playback.cs`)

- `PlayControl()`: stop using the captured local `endDspTime` after
  `_playbackEndDspTime` is assigned. The fade-out deadline (`endDspTime - fadeOut`) and the
  final `while (AudioSettings.dspTime < endDspTime)` must re-read `_playbackEndDspTime` every
  frame.
- `ScheduleNextPlayback()`: don't bake the handover times at coroutine start. Evaluate the
  wait condition per frame against the live field
  (`AudioSettings.dspTime < _playbackEndDspTime - seamlessFadeOut - warmUpTime`) and build
  `newPref.ScheduledStartTime/EndTime` (and `PitchAdjusted` with the then-current
  `AudioSource.pitch`) only **after** the wait completes, immediately before constructing
  `PlaybackHandoverData`. Keep the `TryGetFadeOut` call sites/order unchanged so the fade-out
  override is not consumed twice. `handover.Pitch = StaticPitch` already carries the new pitch
  to the next iteration, whose own `ResolveScheduledTiming` then produces a consistent
  duration.

### Step 3 — hook the pitch API (`AudioPlayer.Pitch.cs`)

- Immediate path of `IAudioPlayer.SetPitch` (fadeTime <= 0): call
  `RecalculateScheduledEndTime()` right after assigning `AudioSource.pitch`.
- `PitchControl()` (pitch fade): call it once per frame inside the loop, and once after the
  loop sets the final pitch. Per-frame recompute is cheap (a few float ops); the
  `SetScheduledEndTime` re-arm can be throttled (e.g. only when the end time moved by more
  than one DSP buffer) if profiling ever shows it matters — not expected to.
- `SetInitialPitch` runs before `ResolveScheduledTiming`, so the initial path needs no change.

### Step 4 — distinguish derived vs. explicit end times

`ISchedulable.SetScheduledEndTime(double dspTime)` is a public API where the caller chose an
**absolute** DSP time; rescaling it on pitch change would violate the contract. Track origin
with a private bool (e.g. `_isEndTimeDerivedFromClip`):

- set `true` in `ResolveScheduledTiming` / when `ScheduleNextPlayback`'s handover pref is
  built and on `ReceiveHandover`;
- set `false` in `ISchedulable.SetScheduledEndTime`;
- `RecalculateScheduledEndTime` only rewrites `_pref.ScheduledEndTime` when the flag is true.
  (`_playbackEndDspTime` for one-shots — where `ScheduledEndTime` stays 0 — is always derived
  and always recomputed.)

### Step 5 — pause/resume correctness

In `RebaseScheduleAfterPause()`, keep sliding `ScheduledStartTime` by the pause duration, but
replace the end-time slide with a call to `RecalculateScheduledEndTime()` once the source is
un-paused (the playhead formula is immune to both the pause gap and any `SetPitch` issued
while paused). The resume path in `PlayControl` already re-arms via `ScheduleEndTime()`; make
sure it re-arms the recomputed value.

## Edge cases to handle / decide

- **Pitch ≈ 0**: playhead doesn't advance; `PitchAdjusted` already special-cases it. The
  recompute should mirror that (skip re-arming, leave end time effectively frozen) instead of
  dividing by ~0.
- **Negative pitch** (`AudioConstant.MinAudioSourcePitch` is -3, reverse playback): the
  playhead moves toward `StartPosition`, so "remaining until end position" is undefined.
  Proposal: when pitch < 0, clear the hardware scheduled end and fall back to the existing
  frame-based stop checks; document that sample-accurate looping requires pitch > 0.
- **Pitch change inside the warm-up window**: covered by Step 1.2 plus the `_nextPlayer`
  propagation in Step 1.5.
- **Pitch change after fade-out already began**: the fade has started at the old deadline;
  recompute moves the end, but re-stretching an in-flight fade is not worth the complexity —
  accept the fade finishing on its original curve, only the stop/handover moment moves.
- **`PitchShiftingSetting.AudioMixer`**: currently a no-op (the mixer write is commented out)
  and mixer pitch wouldn't alter `AudioSource.timeSamples` progression the same way — out of
  scope; only the `AudioSource` mode is affected.
- **Chained mode** (`needNewClip` resets `ScheduledEndTime` to 0): unaffected — the next
  player recomputes from its own picked clip; only the (live) start seam matters, which Step 2
  fixes.

## Test plan (PlayMode, `Assets/Tests/`)

Drive a real `SoundManager` (or the existing test scaffolding) and assert on
`AudioSource.timeSamples` / `AudioSettings.dspTime` deltas:

1. **Loop + pitch up mid-iteration**: next iteration's source starts within one DSP buffer of
   the moment the previous source's playhead hits `endSample`; no silence gap.
2. **Loop + pitch down mid-iteration**: previous iteration is not cut before `endSample`;
   no skipped content at the seam.
3. **Seamless loop with transition time**: handover (fade overlap) begins `fadeOut` before the
   recomputed end, not the original one.
4. **One-shot + pitch change**: `OnEnd` fires when samples run out, not at the stale time
   (both directions).
5. **Pitch fade (`SetPitch(p, fadeTime)`) spanning the seam**: end time converges; seam stays
   gapless.
6. **Pause → `SetPitch` → UnPause**: resumes and ends at the recomputed time.
7. **Explicit `SetScheduledEndTime` then `SetPitch`**: the user's absolute end time is honored
   unchanged (Step 4 flag).
8. **Pitch change during warm-up window** (after `PlayScheduled`, before audible start) with a
   pre-spawned `_nextPlayer`: both players' schedules update.

## Out of scope

- Implementing real mixer-based pitch shifting (`PitchShiftingSetting.AudioMixer`).
- Re-stretching in-flight volume fades when the end time moves.
- Sample-accurate looping under reverse playback (negative pitch).
