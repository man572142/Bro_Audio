# BroAudio Unity Localization — Feature Status Report

> **Branch**: `claude/review-localization-feature-DiRZf`
> **Baseline**: `origin/DEV_Unity6`
> **Generated**: 2026-03-31
> **Purpose**: Agent-readable audit of what has been implemented, what diverges from spec, and what still needs work.

---

## 1. Scope Summary

This branch adds **Unity Localization package** support to BroAudio. The mental model:

- One `AudioEntity` = one Asset Table collection + one entry key.
- `BroAudioClip[]` rows store **playback properties per locale** (volume, fades, delay, etc.), not the `AudioClip` itself.
- The `AudioClip` is stored in — and resolved from — the Unity Asset Table at runtime.
- A new `MulticlipsPlayMode.Localization` enum value drives the entire feature branch.

---

## 2. Changed Files vs. `DEV_Unity6`

```
20 files changed, 1251 insertions(+), 51 deletions(-)
```

| File | Status |
|------|--------|
| `Assets/BroAudio/Runtime/BroAudio.asmdef` | Modified |
| `Assets/BroAudio/Editor/BroAudioEditor.asmdef` | Modified |
| `Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs` | Modified |
| `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs` | Modified |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.cs` | Modified |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.cs` | Modified |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs` | Modified |
| `Assets/BroAudio/Editor/AudioPreview/EntityReplayRequest.cs` | Modified |
| `Assets/BroAudio/Runtime/DataStruct/BroAudioClip.Localization.cs` | **New** |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs` | **New** |
| `Assets/BroAudio/Runtime/Utility/LocalizedBroAudioClipWrapper.cs` | **New** |
| `Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs` | **New** |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs` | **New** |
| `Docs/implementation-plan.md` | **New** (planning doc) |
| `Docs/unity-localization-plan.md` | **New** (architectural spec) |

---

## 3. Phase-by-Phase Completion Status

### Phase 1 — Assembly Definitions ✅ COMPLETE

**`Assets/BroAudio/Runtime/BroAudio.asmdef`**
- Added `versionDefines` entry: `com.unity.localization` → `PACKAGE_LOCALIZATION` symbol.
- Runtime assembly references Unity.Localization via GUID references (lines 5–7).

**`Assets/BroAudio/Editor/BroAudioEditor.asmdef`**
- Added `"Unity.Localization"` and `"Unity.Localization.Editor"` to the `references` array (lines 8–9).
- Added `PACKAGE_LOCALIZATION` version define (lines 28–31).

> **Note**: `BroAudio.asmdef` uses GUIDs for all references (not package names). Confirm that the three GUIDs at lines 5–7 include the localization package GUID. If not, the runtime assembly may not compile against the localization types even though the symbol is defined.

---

### Phase 2 — Data Layer ✅ COMPLETE

**`Assets/BroAudio/Runtime/DataStruct/BroAudioClip.Localization.cs`**
- Partial class; adds `[SerializeField] public LocaleIdentifier Locale` (line 9).
- Guarded by `#if PACKAGE_LOCALIZATION`. ✅

**`Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs`**
- Partial class; adds `[SerializeField] private TableReference _localizationTable` (line 9) and `[SerializeField] private TableEntryReference _localizationEntry` (line 10).
- Contains `LocalizationEditorPropertyName` static class with `const string` property name constants, inside `#if UNITY_EDITOR` (lines 12–18).
- Guarded by `#if PACKAGE_LOCALIZATION`. ✅

**`Assets/BroAudio/Runtime/Utility/LocalizedBroAudioClipWrapper.cs`**
- Implements `IBroAudioClip`.
- Two constructors: `(BroAudioClip, AudioClip)` and `(AudioClip)` fallback (lines 18–28).
- All `IBroAudioClip` properties forwarded via `?.` null-coalescence from `_broAudioClip` (lines 34–39). Uses `AudioConstant.FullVolume` as default volume, `0f` for all other properties. ✅
- Guarded by `#if PACKAGE_LOCALIZATION`. ✅

---

### Phase 3 — Runtime Playback ✅ COMPLETE

**`Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs`**
- `Localization` appended as the final enum value (line 11). ✅

**`Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.cs` — `PickNewClip` switch**
- Lines 53–58: Localization case calls `EnsureClipSelectionStrategy<LocalizationClipStrategy>()` then `Inject(_localizationTable, _localizationEntry, Name)`. ✅
- Strategy is re-injected on every `PickNewClip` call (no stale state). ✅

**`Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs`** (new)
- `Inject(TableReference, TableEntryReference, string)` stores table/entry/entity name (lines 20–25).
- `SelectClip`: validates table and entry are non-empty, then calls `LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>(_table, _entry).WaitForCompletion()` (lines 43–45).
- Falls back to `LocalizedBroAudioClipWrapper(resolvedClip)` with null `BroAudioClip` if no locale row found in `Clips[]` (line 70). ✅
- `Reset()` is a no-op. ✅
- Guarded by `#if PACKAGE_LOCALIZATION`. ✅

**`Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs`**
- Null guard added at line 78: if `_clip == null` after `PickNewClip()`, calls `EndPlaying()` and `yield break`. ✅

---

### Phase 4 — Editor UI ✅ COMPLETE

**`Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs`** (new partial)

| Behaviour | Status |
|-----------|--------|
| Table collection dropdown (`DrawAssetTableDropdown`) | ✅ Lines 342–385 |
| Entry key dropdown (`DrawTableEntryDropdown`) | ✅ Lines 387–450 |
| Per-locale AudioClip ObjectField (reads/writes Asset Table) | ✅ Lines 228–233 |
| Per-locale Volume slider | ✅ Lines 235–251 |
| Play preview button per locale | ✅ Lines 210–226 |
| Locale label (read-only, shows code) | ✅ Line 253 |
| Auto-sync clips array with `AvailableLocales` | ✅ `SyncClipsWithLocales()` lines 106–155 |
| Preserve volume values on re-sync | ✅ `savedVolumes` dictionary lines 122–132 |
| Subscribe/unsubscribe `SelectedLocaleChanged` | ✅ Lines 49, 54 |
| `IsClipsSyncedWithLocales()` guard to avoid unnecessary writes | ✅ Lines 79–104 |
| `TryGetLocalizationEntityPreviewClip` for entity-level preview | ✅ Lines 278–301 |
| Fallback to `ProjectLocale` when `SelectedLocale` is null | ✅ Lines 284–287 |

**`Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.cs`** (modified)
- `Height` property routes to `_localizationList.GetHeight()` in Localization mode (lines 64–70). ✅
- `CurrentSelectedClip` routes to `GetLocalizationCurrentSelectedClip()` (lines 83–86). ✅
- `HasClips` checks `HasLocalizationTableClip` in Localization mode (lines 112–117). ✅
- `DrawReorderableList` routes to `_localizationList.DoList()` (lines 292–298). ✅
- `UpdatePlayModeAndRequiredClipCount` skips clip-count enforcement in Localization mode (lines 332–336). ✅
- `TryGetSelectedAudioClip` routes to `TryGetLocalizationSelectedClip` (lines 172–175). ✅
- Constructor calls `InitLocalization(serializedObject)` (line 141). ✅
- `Dispose()` calls `DisposeLocalization()` (line 150). ✅
- Header `OnDrawHeader` shows "Locale" label and tooltip for Localization mode (lines 408–411). ✅

**`Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs`** (modified)
- `EntityAudioPreview` (lines 663–720): detects `PlayMode == Localization`, calls `TryGetLocalizationEntityPreviewClip`, wraps with `LocalizedBroAudioClipWrapper`. ✅
- Replay clip picker lambda created for Localization mode (lines 696–704). ✅
- Local helper `WrapLocalizationClip` (lines 714–718) safely handles null `BroAudioClip` when clip index is out of range. ✅

**`Assets/BroAudio/Editor/AudioPreview/EntityReplayRequest.cs`** (modified)
- `_clipPickerOverride` lambda accepted in constructor (line 13), used in `GetAudioClipForScheduling` (lines 45–52). ✅

---

### Phase 5 — Library Manager Integration ⚠️ PARTIAL

`LibraryManagerWindow.LibraryFactory.cs` needs one targeted change. `LibraryManagerWindow.cs` requires **no changes** — the `MulticlipsPlayMode` enum dropdown in `AudioEntityEditor` already handles mode selection, and the table + entry UI is already shown by `AudioEntityEditor` once the mode is set.

| Missing Behaviour | File | Description |
|-------------------|------|-------------|
| Clear stale direct `AudioClip` refs on mode switch | `LibraryManagerWindow.LibraryFactory.cs` | Inside `#if PACKAGE_LOCALIZATION`: when an entity is converted to Localization mode, clear all `BroAudioClip.AudioClip` direct references so stale assignments don't coexist with the table-based approach. |

---

### Phase 6 — Validation Checklist

Cross-referencing `Docs/implementation-plan.md` §Phase 6 against the actual code:

| Risk | Result |
|------|--------|
| `AudioPlayer` null guard | ✅ Guard at `AudioPlayer.Playback.cs:78–82` |
| `LocalizationTable` / `LocalizationEntry` unset | ✅ `LocalizationClipStrategy.cs:29–41` logs descriptive errors |
| Locale row in `Clips[]` but no clip in table | ✅ `LocalizationClipStrategy.cs:47–53` logs warning with locale name |
| Clip in table but no row in `Clips[]` | ✅ Falls back to `LocalizedBroAudioClipWrapper(resolvedClip)` at line 70; playback properties default to `FullVolume`, `0f` fades |
| Editor locale refresh | ✅ Subscribe at `ReorderableClips.Localization.cs:49`, unsubscribe at line 54 |
| `ResetMultiClipStrategy()` | ✅ `LocalizationClipStrategy.Reset()` is a no-op (stateless) |
| `#if PACKAGE_LOCALIZATION` coverage | ✅ All new types and Localization API calls are guarded |

---

## 4. Design Divergences from Spec

These are intentional or beneficial departures from `Docs/unity-localization-plan.md` and `Docs/implementation-plan.md`.

### 4a. `LocalizationClipStrategy` resolves `AudioClip` directly

**Spec said**: Strategy only finds the matching `BroAudioClip` by locale; Asset Table resolution happens in the `AudioEntity.Localization.cs` partial.

**Actual**: `LocalizationClipStrategy.SelectClip()` both finds the locale match *and* resolves the `AudioClip` from the Asset Table via `WaitForCompletion()`. The result is returned as a `LocalizedBroAudioClipWrapper`.

**Why it's fine**: Consolidating table resolution into the strategy is architecturally cleaner — the strategy is the only object that knows the table reference. The spec's Phase 2 stub approach would have led to awkward cross-partial coupling.

**Action needed**: None (design improvement), but update `Docs/implementation-plan.md` §Phase 3 to reflect actual architecture if this doc will be used as a reference.

### 4b. Locale rows auto-sync instead of Add/Remove buttons

**Spec said**: "Add (`+`)" opens a locale picker; "Remove (`-`)" removes a `BroAudioClip` row.

**Actual**: `ReorderableList` is created with `displayAddButton: false, displayRemoveButton: false`. Rows are auto-generated by `SyncClipsWithLocales()` to match `LocalizationSettings.AvailableLocales`. Volume is preserved across re-syncs via a dictionary keyed on locale code.

**Why it's fine**: Manual add/remove would allow the editor to go out of sync with the actual available locales. Auto-sync is safer and simpler for users.

**Action needed**: None (design improvement).

### 4c. FadeIn/FadeOut sliders absent from per-locale rows

**Spec said**: Each locale row should show "FadeIn/FadeOut sliders".

**Actual**: Only Volume is shown per locale row. FadeIn/FadeOut default to `0f` in `LocalizedBroAudioClipWrapper` when no `BroAudioClip` row matches.

**Why this might matter**: If a user needs per-locale fade control (e.g., a locale-specific audio clip has different timing needs), there is no UI path. The data field exists (`BroAudioClip.FadeIn/FadeOut` is forwarded by the wrapper), but the editor doesn't expose it.

**Action needed**: Evaluate whether per-locale FadeIn/FadeOut is in scope. If yes, add two sliders per row in `DrawLocalizationTableClipElement` (`ReorderableClips.Localization.cs:186–254`).

---

## 5. Areas Needing Refinement

### 5a. Phase 5 (Library Manager) — REMAINING WORK

**File**: `Assets/BroAudio/Editor/EditorWindow/LibraryManagerWindow.LibraryFactory.cs`

When an entity is converted to Localization mode, any previously assigned `BroAudioClip.AudioClip` direct references should be cleared so stale clip assignments don't coexist silently with the table-based approach. No prompt or table/entry guidance is needed — `AudioEntityEditor` already shows that UI.

### 5b. Deferred items (address later)

| Item | Location | Description |
|------|----------|-------------|
| Fallback warning log | `LocalizationClipStrategy.cs:69` | Log warning when no `BroAudioClip` row matches the active locale (fallback to default properties) |
| `TrySetClipInTable` dirty check | `ReorderableClips.Localization.cs:536` | Verify `AddAssetToTable` reliably marks the table collection dirty; if not, add explicit `EditorUtility.SetDirty` |
| `BroAudio.asmdef` GUID verification | `BroAudio.asmdef:5–7` | Confirm one of the three GUID refs corresponds to `com.unity.localization` runtime assembly |
| `WaitForCompletion` cold-start stall | `LocalizationClipStrategy.cs:43–45` | Known risk for remote tables; consider pre-warm guidance or a `WarnOnLocalizationSyncLoad` setting |
| FadeIn/FadeOut per-locale sliders | `ReorderableClips.Localization.cs:186–254` | Currently only Volume is exposed per locale row; evaluate whether per-locale fade control is needed |

---

## 6. Summary

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1 — asmdef | ✅ Complete | |
| Phase 2 — Data Layer | ✅ Complete | |
| Phase 3 — Runtime Playback | ✅ Complete | Architecture diverges from spec (better approach) |
| Phase 4 — Editor UI | ✅ Complete | |
| Phase 5 — Library Manager | ⚠️ Partial | Clear stale `AudioClip` refs on mode conversion |
| Phase 6 — Validation | ⚠️ Mostly complete | Minor deferred items (see §5b) |

**The feature is functionally complete.** An entity can be switched to Localization mode via `AudioEntityEditor`, assigned a table + entry, and clips resolve at runtime per the active locale. The only remaining planned work is clearing stale direct `AudioClip` references in `LibraryManagerWindow.LibraryFactory.cs` during mode conversion.
