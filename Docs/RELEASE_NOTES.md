# BroAudio – Release Notes (DEV_Unity6 since 3.1.2)

This document summarizes the changes accumulated on the `DEV_Unity6` branch
since tag **3.1.2**. It covers new features, improvements / optimizations,
and bug fixes, and is intended as a draft for the next BroAudio release on
Unity 6.

> Range: `3.1.2..DEV_Unity6`
> Includes everything shipped in the 3.1.3 hotfix plus all subsequent
> work merged into `DEV_Unity6`.

---

## New Features

### Unity Localization integration
A complete integration with Unity's Localization package, allowing
BroAudio entities to drive their clips through locale-aware asset tables.

- New **Localization play mode** for audio entities that selects a clip
  per locale via Unity Localization's `AssetTableCollection`.
- New **deferred preload API** for localization audio assets so locales
  can be loaded ahead of playback.
- New **`SubscribeLocalizedAudioChanged` / `UnsubscribeLocalizedAudioChanged`**
  events for reacting to locale-driven clip changes at runtime.
- Library Manager additions:
    - **"Open Localization Table"** button on the reorderable clip list.
    - Inline locale **table / entry dropdowns** integrated as the first
      row of the clip list.
    - Confirmation dialog when switching an existing entity into
      Localization mode to avoid silently dropping clips.
- Addressables and Localization asset loading APIs are unified, so the
  same code paths cover both delivery modes.
- The **Play Mode** dropdown is now always visible in the Library
  Manager, including when Localization is enabled.

### Audio playback features
- **Change Clip Per Loop** – audio entities can now pick a different
  clip on every loop iteration (#63).
- **Multi-sequence instance support** in `SequenceClipStrategy`, so the
  same sequence-mode entity can drive multiple independent player
  instances without sharing state.
- **Sample-accurate looping** – seamless loops are now driven by
  `AudioSource.PlayScheduled` with a handover between players, giving
  sample-accurate loop boundaries instead of relying on warm-up time.
- **Cross-loop volume fades** – `IAudioPlayer.SetVolume()` fades survive
  loop handovers, so volume transitions remain smooth across sample-
  accurate loop boundaries.
- **`IAudioPlayer.GetVolume()`** and supporting chaining APIs were
  added.
- **Editor crossfade preview** for Chained play mode combined with
  Seamless Loop.

### Editor / Tooling
- BroAudio editor windows are now also accessible under
  **Window ▸ Audio**, alongside the existing **Tools ▸ BroAudio** menu.

---

## Improvements & Optimizations

### Playback engine
- Refactored the **fading pipeline**: `_clipVolume` fades are now owned
  by `Fader` coroutines, redundant `Fader.StopCoroutine()` calls were
  removed, and coroutine management was made private to the component
  that owns them.
- `StopControl` now honors `overrideFade` when a natural fade-out is
  already in progress, so explicit stop requests can shorten or
  lengthen an in-flight fade.
- Loop handover plumbing simplified and renamed for clarity (better
  handover / scheduling identifier names).
- Restored handover-clip plumbing and adjusted the loop end time for
  pitch so scheduled handovers stay aligned when pitch ≠ 1.
- `ScheduledEndTime` is now deferred to the new player when the next
  loop will play a different clip, avoiding wrong end-time stamping
  on the outgoing player.
- Removed the first-iteration `warmUpTime`, eliminating the up-front
  latency that previously protected the first loop.
- DSP buffer warm-up is now cached for scheduled playback, removing a
  redundant warm-up cost per scheduled play.
- Clip delay was refactored to use scheduled start times and to ensure
  the audio track is available after the scheduled wait.
- General playback validation paths were tightened (and an
  `audioTypePref` typo was fixed along the way).

### Localization runtime
- `OnSelectedLocaleChanged` now performs a **lazy reload** instead of
  building a `List<SoundID>`, eliminating per-locale allocations.
- Addressables are locked while in Localization mode and per-locale
  assignment is routed through `AssetTableCollection`, preventing two
  loading strategies from fighting over the same asset.
- A warning is logged when localized audio is played without having
  been preloaded.
- Localization mode-switch logic moved into a `Localization` partial
  class; localization editor code was de-duplicated (extracted
  constants, cached labels, centralized table lookup).
- Localization clip selection migrated into a dedicated
  `LocalizationClipStrategy`, and the localization header was unified
  with the base `OnDrawHeader`.
- `ProjectLocale` is used as a fallback for the localization preview
  when `SelectedLocale` is null.

### Editor / UX
- `LastEditAudioAsset` moved to `EditorPrefs` so it no longer churns
  version control.
- Localization clip element layout now matches the standard clip list,
  including the table / entry row layout.
- Chained play mode editor preview was refactored to match runtime
  behavior, including:
    - Playing the end clip on stop in the Chained editor preview.
    - Removing redundant scheduling and delay for the end clip in the
      editor preview.
    - Only transitioning to End when the entity preview button is
      clicked in `ChainedPlayMode`.
    - Replacing scattered booleans with a typed `PlaybackStage` field
      and flattening preview conditionals.

### Robustness
- Release verbs are now guarded against a torn-down `SoundManager`, so
  shutdown / domain-reload paths do not throw.
- Addressable cleanup is now scoped via conditional compilation so it
  no longer runs in builds that do not include Addressables.

---

## Bug Fixes

### Playback / Looping
- Fix **NRE in `BeginHandover`** when `Stop` is called inside the
  `ChainedPlayMode` loop-handover window (#57).
- Fix **NRE in `StopControl`** when the `RequestNextPlayer` delegate
  was previously cleared in `PlayControl`.
- Fix `StopControl` continuing to fire `onUpdate` on the old player
  after an end-handover.
- Fix scheduled loop handover not being canceled when `Stop` is called.
- Fix seamless-loop timing where scheduled next playback didn't start
  before the fade-out.
- Fix handover looping: seamless-loop timing and chained-mode clip
  assignment now line up correctly.
- Fix Chained play mode not proceeding to the end clip as expected.
- Fix `PlaybackIndicator` not being repositioned correctly after a
  cross-fade.
- Suppress the "invalid ID" error when the audio player is stopped in
  the same frame it was played.

### Localization
- Fix `IsLoaded` reporting wrong state for Localization entities; drop
  redundant `Undo`/`SaveAssets` in `TrySetClipInTable`.
- Fix localization clip properties being wiped on domain reload.
- Fix locale list being empty after domain reload.
- Fix localization mode preview ignoring clip properties.
- Fix entity-level localization preview using the editor API instead
  of the runtime path.
- Fix `NullReferenceException` when previewing localization clips.
- Fix clip properties not displaying in Localization play mode.
- Fix right-click "Preview" of a localization clip applying master
  volume and pitch.
- Fix localization audio preview not updating its clip index or
  button state.

### Editor
- Fix IMGUI layout exception when switching `MulticlipsPlayMode`.
- Add the localization assembly reference and several missing script
  `.meta` files so the package compiles cleanly with Unity
  Localization installed.

---

## Notes for upgraders

- Subscription API names were finalized as
  **`SubscribeLocalizedAudioChanged` / `UnsubscribeLocalizedAudioChanged`**.
  Earlier in-development names (`SubscribeLocalizedClipChanged` /
  `UnsubscribeLocalizedClipChanged`) were renamed before release.
- Loops now run on `PlayScheduled` + handover. Behavior that depended
  on the previous `warmUpTime` on the first loop iteration may need
  to be re-tested.
- Localization mode locks Addressables for its entries; mixing manual
  Addressable management with Localization mode on the same entity is
  no longer supported by design.
