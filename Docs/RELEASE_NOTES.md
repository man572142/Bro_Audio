# BroAudio – Release Notes (DEV_Unity6 since 3.1.2)

This document summarizes the user-facing changes accumulated on the
`DEV_Unity6` branch since tag **3.1.2**. It covers new features, plus
the improvements and bug fixes that affect behavior already shipped in
3.1.2. Internal churn from the development of the new features (e.g.
fixes to never-released localization or handover code) is intentionally
omitted.

---

## New Features

### Unity Localization integration
A complete integration with Unity's Localization package, allowing
BroAudio entities to drive their clips through locale-aware asset
tables.

- New **Localization play mode** for audio entities that selects a clip
  per locale via Unity Localization's `AssetTableCollection`.
- **Deferred preload API** for localization audio assets so locales can
  be loaded ahead of playback.
- **`SubscribeLocalizedAudioChanged` / `UnsubscribeLocalizedAudioChanged`**
  runtime events for reacting to locale-driven clip changes.
- Library Manager additions:
    - **"Open Localization Tables"** button on the reorderable clip list.
    - Inline locale **table / entry dropdowns** integrated as the first
      row of the clip list.
    - Confirmation dialog when switching an existing entity into
      Localization mode to avoid silently dropping clips.
- Addressables and Localization asset loading APIs are unified, so the
  same code paths cover both delivery modes.

### Audio playback features
- **Change Clip Per Loop** – audio entities can now pick a different
  clip on every loop iteration (#63).
- **Sample-accurate looping** – seamless loops are now driven by
  `AudioSource.PlayScheduled` with a handover between players, giving
  sample-accurate loop boundaries.
- **Cross-loop volume fades** – volume changes via
  `IAudioPlayer.SetVolume()` now carry across loop handovers, so fades
  remain smooth across sample-accurate loop boundaries.
- **Multi-sequence instance support** in `SequenceClipStrategy`, so the
  same sequence-mode entity can drive multiple independent player
  instances without sharing state.
- **`IAudioPlayer.GetVolume()`** and supporting chaining APIs.
- **Editor crossfade preview** for Chained play mode combined with
  Seamless Loop.

### Editor / Tooling
- BroAudio editor windows are now also accessible under
  **Window ▸ Audio ▸ BroAudio**, alongside the existing
  **Tools ▸ BroAudio** menu.
- The **Play Mode** dropdown is now always visible in the Library
  Manager.

---

## Improvements & Optimizations

- `StopControl` now honors `overrideFade` when a natural fade-out is
  already in progress, so an explicit stop request can shorten or
  lengthen an in-flight fade.
- Chained play mode editor preview was reworked to better match runtime
  behavior:
    - End clip is now played on stop in the editor preview.
    - The preview only transitions to the End clip when the entity
      preview button is clicked.
    - Redundant scheduling and delay for the end clip in the editor
      preview were removed.
- Release verbs are now guarded against a torn-down `SoundManager`, so
  shutdown / domain-reload paths no longer throw.
- `LastEditAudioAsset` moved to `EditorPrefs` so it no longer churns
  version control.
- Playback validation paths were tightened.
- Addressable cleanup is now scoped via conditional compilation so it
  no longer runs in builds that do not include Addressables.
- Renamed the two Addressable toggles for clarity: the Clips tab toggle
  is now **"Addressable Audio Clips"** and the Overall tab toggle is now
  **"Addressable Entity Asset"**, each tooltip naming the other. The
  entity-asset toggle is now hidden by default and can be re-enabled
  under **Tools ▸ BroAudio ▸ Preferences ▸ Addressable Settings**.
  Existing `AddressableAssetSettings` entries are unaffected.

---

## Bug Fixes

- Fix IMGUI layout exception when switching `MulticlipsPlayMode`.
- Suppress the "invalid ID" error when the audio player is stopped in
  the same frame it was played.
- Fix the `audioTypePref` typo in editor preference handling.

---

## Notes for upgraders

- Seamless loops now run on `PlayScheduled` + handover. Behavior that
  depended on the previous first-iteration `warmUpTime` may need to be
  re-tested.
- Localization mode locks Addressables for its entries; mixing manual
  Addressable management with Localization mode on the same entity is
  not supported by design.
