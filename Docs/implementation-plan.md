# BroAudio Unity Localization — Implementation Plan

> Source spec: `Docs/unity-localization-plan.md`
> No code is written in this document. Each phase is assigned to an isolated subagent.

---

## Dependency Order

```
Phase 1 (asmdef)
    └─► Phase 2 (Data Layer)
            └─► Phase 3 (Runtime Playback)
                    ├─► Phase 4 (Editor UI)
                    └─► Phase 5 (Library Manager)
                                └─► Phase 6 (Validation)
```

Phases 4 and 5 can run in parallel after Phase 3 completes.

---

## Phase 1 — Assembly Definition & Scripting Symbol

**Subagent:** `general-purpose`
**Isolation rationale:** Only touches `.asmdef` JSON files. No C# knowledge needed; completely self-contained.

### Files to modify
| File | Change |
|------|--------|
| `Assets/BroAudio/Runtime/BroAudio.asmdef` | Add `com.unity.localization` as an optional reference. Add a `versionDefines` entry that sets `PACKAGE_LOCALIZATION` when `com.unity.localization` ≥ 1.0.0, mirroring the existing `PACKAGE_ADDRESSABLES` entry. |
| `Assets/BroAudio/Editor/BroAudioEditor.asmdef` | Add `Unity.Localization` and `Unity.Localization.Editor` assembly references. Add the same `PACKAGE_LOCALIZATION` version define. |

### Acceptance criteria
- Both `.asmdef` files compile without error when the Localization package is absent (optional ref).
- `PACKAGE_LOCALIZATION` symbol is defined when `com.unity.localization` is present.
- Pattern is identical to the existing `PACKAGE_ADDRESSABLES` entries.

### Context files to read
- `Assets/BroAudio/Runtime/BroAudio.asmdef`
- `Assets/BroAudio/Editor/BroAudioEditor.asmdef`

---

## Phase 2 — Data Layer

**Subagent:** `general-purpose`
**Isolation rationale:** Purely additive data structures. No strategy pattern, no editor, no Unity Localization runtime calls — only new fields and a wrapper class.

### Files to create
| File | Purpose |
|------|---------|
| `Assets/BroAudio/Runtime/DataStruct/BroAudioClip.Localization.cs` | Partial class adding `LocaleIdentifier Locale` field, guarded by `#if PACKAGE_LOCALIZATION`. |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs` | Partial class adding `TableReference LocalizationTable` and `TableEntryReference LocalizationEntry` serialized fields. The `PickNewClip` case body will be stubbed (throws `NotImplementedException`) and completed in Phase 3. |
| `Assets/BroAudio/Runtime/Utility/LocalizedBroAudioClipWrapper.cs` | New class implementing `IBroAudioClip`. Wraps a `BroAudioClip` (for playback properties) and an externally resolved `AudioClip`. Forwards all `IBroAudioClip` members to the inner clip except `GetAudioClip()`, which returns the resolved clip. |

### Key contracts to preserve
- `BroAudioClip` is a `class` (not struct); the new partial must not change that.
- `IBroAudioClip` members: `GetAudioClip()`, `IsValid()`, `IsSet`, `Volume`, `Delay`, `StartPosition`, `EndPosition`, `FadeIn`, `FadeOut`. `LocalizedBroAudioClipWrapper` must implement all of them.
- All new code is inside `#if PACKAGE_LOCALIZATION`.

### Context files to read
- `Assets/BroAudio/Runtime/DataStruct/BroAudioClip.cs` (full file — interface + class)
- `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.cs` (fields + `PickNewClip` signature only)
- `Assets/BroAudio/Runtime/DataStruct/BroAudioClip.Addressables.cs` (partial class pattern reference)

---

## Phase 3 — Runtime Playback

**Subagent:** `general-purpose`
**Isolation rationale:** Requires understanding the strategy pattern and `PickNewClip` switch, but no editor APIs. Context is bounded to Runtime/Utility/ClipSelection and AudioPlayer.

### Files to modify / create
| File | Change |
|------|---------|
| `Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs` | Append `Localization` as the last enum value. |
| `Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs` | **New file.** Implements `IClipSelectionStrategy`. `SelectClip()` finds the `BroAudioClip` in the array whose `Locale` matches `LocalizationSettings.SelectedLocale`. Returns `null` and logs the warning `"[BroAudio] No BroAudioClip configured for locale '{locale}' on entity '{name}'."` if no match. Does **not** resolve `AudioClip`. `Reset()` is a no-op (stateless). |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs` | Replace the Phase 2 stub with the full `Localization` case in `PickNewClip()` (see spec §2 — steps 1–6). Uses `WaitForCompletion()`. Returns `LocalizedBroAudioClipWrapper`. Fails fast with a descriptive error if `LocalizationTable` or `LocalizationEntry` is unset. |
| `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs` | **Audit only, patch if needed.** Verify that after `_clip = _pref.PickNewClip()` the code performs a null check before calling `_clip.GetAudioClip()` (currently line ~90 does not). Add guard: if `_clip == null`, skip playback gracefully (recycle player, return). |

### Key contracts to preserve
- `IClipSelectionStrategy.SelectClip(BroAudioClip[] clips, ClipSelectionContext context, out int index)` — index is unused in Localization mode, output `-1`.
- `PickNewClip` switch must use `EnsureClipSelectionStrategy<LocalizationClipStrategy>()` matching the existing helper pattern.
- All new code is inside `#if PACKAGE_LOCALIZATION` except the `AudioPlayer` null guard (unconditional safety fix).

### Context files to read
- `Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs`
- `Assets/BroAudio/Runtime/Utility/ClipSelection/IClipSelectionStrategy.cs`
- `Assets/BroAudio/Runtime/Utility/ClipSelection/SingleClipStrategy.cs` (pattern reference)
- `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.cs` (full `PickNewClip` switch)
- `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Addressables.cs` (pattern reference)
- `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs` (null safety audit)
- Phase 2 output: `BroAudioClip.Localization.cs`, `AudioEntity.Localization.cs`, `LocalizedBroAudioClipWrapper.cs`

---

## Phase 4 — Editor UI (Clip List)

**Subagent:** `general-purpose`
**Isolation rationale:** Purely editor-side. Depends on Phase 3 data types but touches no runtime logic. The Unity Localization editor API and `ReorderableList` drawing are the only concerns.

### Files to create
| File | Purpose |
|------|---------|
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs` | New partial class of `ReorderableClips`, guarded by `#if PACKAGE_LOCALIZATION`. |

### UI behaviour to implement (inside the new partial)
| Area | Behaviour |
|------|-----------|
| **Header** | When `CurrentPlayMode == Localization`: draw `TableReference` ObjectField (→ `AudioEntity.LocalizationTable`) and a `TableEntryReference` dropdown populated from the selected table's entry keys (→ `AudioEntity.LocalizationEntry`). |
| **Each row** | Locale label (read-only, from `BroAudioClip.Locale`). `AudioClip` ObjectField — reads/writes through the Localization editor API to the Asset Table (never writes to `BroAudioClip.AudioClip`). Play preview button (resolves from table). Volume slider (→ `BroAudioClip.Volume`). FadeIn/FadeOut sliders. |
| **Add (`+`)** | Opens a locale picker populated from all locales in `LocalizationSettings`. Appends a new `BroAudioClip` with `Locale` set. Does **not** write to the Asset Table. |
| **Remove (`-`)** | Removes the `BroAudioClip` row from `Clips[]`. Does **not** remove anything from the Asset Table. |
| **Locale refresh** | Subscribe to `LocalizationSettings.SelectedLocaleChanged` (or equivalent editor event) to repaint the list when the editor locale changes. |

### Context files to read
- `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.cs` (full file — drawing helpers, event hooks, existing `#if PACKAGE_ADDRESSABLES` blocks)
- Phase 2 output: `BroAudioClip.Localization.cs`, `AudioEntity.Localization.cs`
- Phase 3 output: `MulticlipsPlayMode.cs` (to know the `Localization` enum value)

---

## Phase 5 — Library Manager Integration

**Subagent:** `general-purpose`
**Isolation rationale:** Touches only `LibraryManagerWindow.LibraryFactory.cs`. Small, self-contained change. Runs in parallel with Phase 4.

> **Scope clarification**: Exposing "Localization" as a play mode option in the Library Manager UI is **not needed** — the `MulticlipsPlayMode` enum dropdown in `AudioEntityEditor` already handles mode selection. Similarly, prompting the developer to assign a table + entry key after conversion is **not needed** — `AudioEntityEditor` already displays the table and entry UI once the mode is set.

### Files to modify
| File | Change |
|------|---------|
| `Assets/BroAudio/Editor/EditorWindow/LibraryManagerWindow.LibraryFactory.cs` | Inside `#if PACKAGE_LOCALIZATION`: when an existing entity is converted to Localization mode, clear all `BroAudioClip.AudioClip` direct references so stale audio clip assignments don't linger alongside the table-based approach. |

### Context files to read
- `Assets/BroAudio/Editor/EditorWindow/LibraryManagerWindow.LibraryFactory.cs`
- Phase 3 output: `MulticlipsPlayMode.cs` (Localization enum value)

---

## Phase 6 — Validation & Risk Mitigation

**Subagent:** `general-purpose`
**Isolation rationale:** Read-only audit across all prior phases. Produces a checklist of found issues; fixes are delegated back to the relevant phase subagent or handled inline.

### Checklist to verify
| Risk (from spec §6) | Verification method |
|---------------------|---------------------|
| `AudioPlayer` null guard | Confirm `_clip == null` path after `PickNewClip()` skips playback without exception. |
| `LocalizationTable` / `LocalizationEntry` unset | Confirm `PickNewClip()` fails fast with a descriptive error (not `NullReferenceException`). |
| Locale row in `Clips[]` but no clip in table | Confirm step 5 of `PickNewClip()` logs the correct warning and returns null. |
| Clip in table but no row in `Clips[]` | Confirm `LocalizationClipStrategy` logs the correct warning and returns null. |
| Editor locale refresh | Confirm `SelectedLocaleChanged` subscription is set up and torn down (no leak). |
| `ResetMultiClipStrategy()` | Confirm `LocalizationClipStrategy.Reset()` is sufficient (stateless — no locale cache). |
| `#if PACKAGE_LOCALIZATION` coverage | Confirm every new type and reference to Localization APIs is inside the guard. |
| Sequence/Random + Localization out-of-scope | Confirm a code comment exists at the `Localization` case in `PickNewClip()` noting this is out of scope. |

### Context files to read
All files produced or modified in Phases 1–5.

---

## File Manifest (all phases combined)

### New files
```
Assets/BroAudio/Runtime/DataStruct/BroAudioClip.Localization.cs
Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs
Assets/BroAudio/Runtime/Utility/LocalizedBroAudioClipWrapper.cs
Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs
Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs
```

### Modified files
```
Assets/BroAudio/Runtime/BroAudio.asmdef
Assets/BroAudio/Editor/BroAudioEditor.asmdef
Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs
Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs      ← null guard only
Assets/BroAudio/Editor/EditorWindow/LibraryManagerWindow.cs
Assets/BroAudio/Editor/EditorWindow/LibraryManagerWindow.LibraryFactory.cs
```
