# Comb-Filtering vs. Scheduled Playback — Findings & Improvement Plan

## TL;DR

**No, the comb-filtering prevention does not take scheduled playback into account.**

Comb-filtering decides whether to reject a play by comparing **game-clock** timestamps
(`Time.unscaledTime`, via `TimeExtension.UnscaledCurrentFrameBeganTime`), captured at the
moment `Play()` is invoked / when the player actually starts. Scheduled playback, on the
other hand, lives entirely on the **DSP clock** (`AudioSettings.dspTime`) and defers the
audible onset to a future time. The two clocks are never reconciled inside the
comb-filtering rule, so a sound that is *scheduled* (or delayed) to be audible far in the
future is treated as if it were sounding "right now." This produces false rejections in
both directions:

- A new immediate play can be rejected because a previously-queued **scheduled** instance
  of the same sound is still sitting in the comb-filtering preventer with a `0`
  (not-yet-started) timestamp, which the rule reads as "same frame."
- A new **scheduled** (or `SetDelay`/`clip.Delay`) play can be rejected because an instance
  that is actually audible *now* was registered moments earlier — even though the new one
  won't be heard for seconds.

Comb filtering is fundamentally about whether two copies of the same clip become *audible*
within a ~40 ms window. With scheduling, the audible onset is `ScheduledStartTime` (a DSP
time), not the `Play()` call time — and that is precisely the value the current rule ignores.

---

## How the two features work today

### Comb-filtering rule

- Config & defaults: `Assets/BroAudio/Runtime/Player/PlaybackGroup/DefaultPlaybackGroup.cs:27-50`
  (`_combFilteringTime`, `_ignoreCombFilteringIfSameFrame`, `_ignoreIfDistanceIsGreaterThan`,
  `_logCombFilteringWarning`). Default window: `RuntimeSetting.CombFilteringPreventionInSeconds = 0.04f` (40 ms).
- Entry point: validation runs in `SoundManager.IsPlayable(...)` →
  `validator.IsPlayable(id, position)` **before** a player is extracted
  (`Assets/BroAudio/Runtime/SoundManager/SoundManager.Playback.cs:45-67`).
- The rule itself: `DefaultPlaybackGroup.HasPassedCombFilteringRule(...)`
  (`DefaultPlaybackGroup.cs:79-131`). The decisive comparison:

  ```csharp
  int time = TimeExtension.UnscaledCurrentFrameBeganTime;     // game clock, "now"
  int previousPlayTime = previousPlayer.PlaybackStartingTime; // game clock, prev start (0 if not started yet)
  bool previousIsInQueue = Mathf.Approximately(previousPlayTime, 0f);
  float difference = time - previousPlayTime;
  bool isSameFrame = previousIsInQueue || Mathf.Approximately(difference, 0f);
  ...
  bool HasPassedCombFilteringTime() => difference >= TimeExtension.SecToMs(_combFilteringTime);
  ```

- Tracking: every accepted play registers its player in
  `_combFilteringPreventer[id] = player` at enqueue time
  (`SoundManager.Playback.cs:80-81`), i.e. immediately — regardless of any scheduled start.
  The "previous player" is looked up via
  `TryGetPreviousPlayerFromCombFilteringPreventer` (`SoundManager.Playback.cs:256-260`).
- The timestamp it compares against, `PlaybackStartingTime`, is stamped from the game clock
  and only **after** the scheduled wait completes:
  `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs:158-160`:

  ```csharp
  if (!HasStartedPlaying)
  {
      PlaybackStartingTime = TimeExtension.UnscaledCurrentFrameBeganTime; // never the DSP/scheduled time
      ...
  }
  ```

  Until then `PlaybackStartingTime == 0` (`AudioPlayer.Playback.cs:28-29, 485`).

### Scheduled playback

- Public API: `SetScheduledStartTime(double dspTime)`, `SetScheduledEndTime(double dspTime)`,
  `SetDelay(float)` (`Assets/BroAudio/Runtime/BroAudioChainingMethod.cs:88-98`).
- Stored on the preference struct: `PlaybackPreference.ScheduledStartTime / ScheduledEndTime`
  (`Assets/BroAudio/Runtime/Player/PlaybackPreference.cs:17-18`).
- Driven on the DSP clock: `AudioPlayer.SchedulePlayback()` calls
  `AudioSource.PlayScheduled(...)` and computes `_secondsUntilScheduledStart`
  (`Assets/BroAudio/Runtime/Player/AudioPlayer.Scheduling.cs:12-25`). `SetDelay` and
  `clip.Delay` both funnel into `ScheduledStartTime = AudioSettings.dspTime + delay`
  (`AudioPlayer.Scheduling.cs:73-84`).
- The playback coroutine waits out the schedule **before** stamping `PlaybackStartingTime`
  (`AudioPlayer.Playback.cs:136-160`).

### Where the clocks diverge

| | Comb-filtering rule | Scheduled playback |
|---|---|---|
| Clock | `Time.unscaledTime` (game) | `AudioSettings.dspTime` (audio) |
| Reference moment | `Play()` call / actual start | future `ScheduledStartTime` |
| Recorded for next comparison | `PlaybackStartingTime` (game clock, or `0` while waiting) | `ScheduledStartTime` (never read by the rule) |
| Evaluated | synchronously in `IsPlayable`, before scheduling resolves | asynchronously, frames/seconds later |

---

## Concrete failure scenarios

1. **Scheduled previous blocks immediate current.**
   `Play(A).SetScheduledStartTime(dsp + 10)` then `Play(A)` in the same frame. The scheduled
   player is in `_combFilteringPreventer` but its `PlaybackStartingTime` is still `0`, so
   `previousIsInQueue == true → isSameFrame == true`. With the default
   `_ignoreCombFilteringIfSameFrame = false` and same/global position, the *immediate* second
   play is rejected — even though the first one is silent for another 10 seconds.

2. **Immediate previous blocks scheduled current.**
   `Play(A)` (audible now) then `Play(A).SetScheduledStartTime(dsp + 10)`. The new play is
   validated against "now," `difference ≈ 0`, and is rejected — despite being inaudible for
   10 seconds, well outside the 40 ms comb window.

3. **`SetDelay` / `clip.Delay`.** Same as (2): both resolve to a future `ScheduledStartTime`,
   so delayed one-shots of the same clip get false-rejected by recent immediate plays (and
   vice-versa).

In all three, the rule should be comparing the **audible onset times** (DSP), which are
seconds apart, not the `Play()` call times, which are ~0 ms apart.

---

## Improvement plan

### Goal
Make the comb-filtering rule reason about the **time a clip actually becomes audible**, so
scheduled / delayed plays are compared on the same timeline and only rejected when their
audible onsets truly fall within the comb window.

### Design

1. **Record an audible-onset timestamp on the DSP clock.**
   - Add a property such as `double AudioPlayer.ScheduledOrImmediateStartDspTime` (name TBD)
     that resolves to:
     - `_pref.ScheduledStartTime` when `> 0` (covers `SetScheduledStartTime`, `SetDelay`,
       and the `clip.Delay` path that already writes `ScheduledStartTime` in
       `SetClipDelayIfNotScheduled`), otherwise
     - `AudioSettings.dspTime` captured at enqueue time for immediate plays.
   - Populate it in `SetPlaybackData` / at enqueue (`SoundManager.Playback.cs` around the
     `_combFilteringPreventer[id] = player` write) so the value exists *before* the
     comb-filtering lookup can read it for the next play — not after the scheduled wait.
   - Note: `clip.Delay` is only folded into `ScheduledStartTime` inside the playback
     coroutine (`SetClipDelayIfNotScheduled`, `AudioPlayer.Scheduling.cs:78-84`). To cover
     the delay case at validation time, resolve the effective delay (max of `clip.Delay` and
     any scheduled value) when computing the onset stamp, rather than relying on the
     coroutine having run.

2. **Compare onsets on one clock inside the rule.**
   - In `DefaultPlaybackGroup.HasPassedCombFilteringRule(previousPlayer, currentPlayPos)`,
     replace the game-clock `time`/`previousPlayTime` math with a DSP-clock comparison:
     `difference = currentOnsetDspTime - previousOnsetDspTime` (absolute value — a new play
     scheduled *before* an existing later one should also be considered), then
     `HasPassedCombFilteringTime() => Mathf.Abs(difference) >= _combFilteringTime`.
   - The current play's onset must be passed into the rule. `PlaybackGroup.IsPlayable`
     currently forwards only `(id, position)`; thread the resolved current-onset DSP time
     through (either compute it in the validator from the pending preference, or pass it as
     a parameter). Keep the existing position/global-distance escape hatches unchanged.

3. **Rework the "same frame" concept.**
   - `previousIsInQueue` (previous `PlaybackStartingTime == 0`) currently conflates "not yet
     started because it's waiting on a long schedule" with "queued to start this very frame."
     With DSP onsets available, "same frame / same instant" becomes a genuine
     `Mathf.Abs(onsetDifference) < epsilon` test, and `_ignoreCombFilteringIfSameFrame`
     keeps its original meaning (two truly simultaneous triggers) without misfiring on
     far-future schedules.

4. **Keep the 40 ms semantics.** The window (`_combFilteringTime`) is a real-seconds value,
   which maps cleanly onto DSP-time deltas (also seconds) — drop the `SecToMs` conversion
   that existed only because the old comparison was in integer milliseconds of game time.

### Touch points
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs` — add the onset-DSP property;
  ensure it's set independently of the deferred `PlaybackStartingTime`.
- `Assets/BroAudio/Runtime/Player/PlaybackGroup/DefaultPlaybackGroup.cs:79-131` — rewrite the
  time comparison to use DSP onsets.
- `Assets/BroAudio/Runtime/Player/PlaybackGroup/PlaybackGroup.cs:54-67` — thread the current
  play's onset time into the rule.
- `Assets/BroAudio/Runtime/SoundManager/SoundManager.Playback.cs:45-81` — compute/stamp the
  current onset before registering in `_combFilteringPreventer`.
- Consider `AudioPlayer.Scheduling.cs` `SetClipDelayIfNotScheduled` so the `clip.Delay`
  onset is known at validation time, not only inside the coroutine.

### Edge cases to cover
- Pure immediate plays must behave exactly as before (onset = `dspTime` now → deltas in ms,
  same rejections).
- `SetScheduledStartTime` called *after* `Play()` (the re-schedule path in
  `AudioPlayer.Scheduling.cs:27-48`) — onset stamp should update so a later comb check sees
  the new time.
- Looping plays, which also write `ScheduledStartTime` (`AudioPlayer.Playback.cs:126-130`).
- `ScheduledEndTime` does not affect comb filtering (onset only) — leave untouched.
- Preventer entry lifetime: a far-future scheduled player stays in `_combFilteringPreventer`
  until it stops (`SoundManager.Playback.cs:135-137`); confirm that's still acceptable, or
  evict/skip entries whose onset is already comb-window-past relative to "now."

### Validation
- Unit/PlayMode tests under `Assets/Tests/` (add if absent) covering: immediate+immediate
  (still rejected within window), immediate+scheduled-far (both accepted), scheduled-far +
  immediate (accepted), two scheduled within 40 ms of each other (rejected), `SetDelay` and
  `clip.Delay` parity with explicit scheduling.
- Manual check via Library Manager / a test scene: fire the same clip immediately and with a
  multi-second `SetDelay`; confirm no false "rejected by the [Comb Filtering Time] rule"
  warning and that both are audible.

### Risks / open questions
- Driving `AudioPlayer` in tests needs an `IAudioMixerPool` test double and a live
  `SoundManager` for `PlaybackPreference` (per project notes) — onset resolution should avoid
  adding new hard dependencies on `SoundManager.Instance` inside the rule.
- Decide whether comb rejection for scheduled plays should be evaluated at *enqueue* time
  (current behavior, simple) or re-evaluated at the resolved DSP onset (more accurate if
  many plays are queued in between). Recommendation: keep enqueue-time evaluation but on the
  correct DSP onset timeline — it fixes the reported bug without restructuring the queue.
