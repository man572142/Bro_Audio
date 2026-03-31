# Unity Localization Support for BroAudio — Implementation Plan

## Clarified Mental Model

One entity = one table + one entry key. That entry has one clip per locale in the Asset Table. Each locale maps to one `BroAudioClip` in the entity, which stores the **playback properties** (Volume, FadeIn, FadeOut, etc.) for that locale. The actual `AudioClip` is never stored in the entity — it's always resolved from the table at runtime. In the editor, the list shows one row per locale, where each row's clip field is a **live proxy** that reads/writes to the Asset Table.

---

## Architecture Overview

```
AudioEntity (ScriptableObject)
├── PlayMode = Localization
├── LocalizationTable  ──────────────────────────────► AssetTable (Unity)
├── LocalizationEntry  ─────────────────────────────┐
└── Clips[]                                          │
     ├── BroAudioClip { Locale=en, Volume, Fade... } │
     ├── BroAudioClip { Locale=ja, Volume, Fade... } ├─► Entry key → per-locale AudioClip
     └── BroAudioClip { Locale=zh, Volume, Fade... } │
                                                     ▼
                                  Runtime: WaitForCompletion() → AudioClip
```

---

## Confirmed Design Decisions

| Question | Decision |
|----------|----------|
| Multiple entries per entity? | No — single entry only. One table + one entry key per entity. |
| Table reference scope | One table per entity, shared across all locale rows. |
| Per-clip playback properties | Yes — each locale has its own `BroAudioClip` with Volume, FadeIn, FadeOut, StartPosition, EndPosition. |
| Missing locale fallback | Log a warning and skip playback. |
| Async loading | `WaitForCompletion()` (synchronous). Future async API deferred. |
| Locale picker (add row) | Shows all locales defined in project's Localization Settings. |
| `BroAudioClip` type | Class. |
| `Localization` enum position | Appended at the end of `MulticlipsPlayMode`. |

---

## 1. Data Layer

### `BroAudioClip.Localization.cs` — New Partial Class (`#if PACKAGE_LOCALIZATION`)

Adds one new field to `BroAudioClip`:

- `LocaleIdentifier Locale` — identifies which locale this clip row belongs to.

The existing `AudioClip AudioClip` field remains in the class but is **always null** in Localization mode — it is not used at runtime. All other fields (Volume, Delay, StartPosition, EndPosition, FadeIn, FadeOut) remain fully used.

### `AudioEntity.Localization.cs` — New Partial Class (`#if PACKAGE_LOCALIZATION`)

Adds two new serialized fields at the entity level:

- `TableReference LocalizationTable` — which Asset Table to use.
- `TableEntryReference LocalizationEntry` — which entry key within that table.

Also contains the `Localization` case logic for `PickNewClip()` (see §2).

### `LocalizedBroAudioClipWrapper.cs` — New Wrapper Class (`#if PACKAGE_LOCALIZATION`)

A lightweight class implementing `IBroAudioClip` that wraps a `BroAudioClip` and an externally resolved `AudioClip`. This avoids mutating ScriptableObject data at runtime:

- Forwards all playback properties (Volume, Fade, StartPosition, etc.) to the wrapped `BroAudioClip`.
- Returns the resolved `AudioClip` from the table on the `AudioClip` property.

This is the object returned by `PickNewClip()` in Localization mode — never the raw `BroAudioClip` directly.

---

## 2. Runtime Playback

### `MulticlipsPlayMode.cs`

Append `Localization` as the last value in the enum.

### `LocalizationClipStrategy.cs` — New Strategy (`#if PACKAGE_LOCALIZATION`)

Implements `IClipSelectionStrategy`. Given `BroAudioClip[] clips` (one per locale), finds the one whose `Locale` matches `LocalizationSettings.SelectedLocale`. If no match is found, returns null and logs a warning:

> `"[BroAudio] No BroAudioClip configured for locale '{locale}' on entity '{name}'."`

The strategy does **not** resolve the `AudioClip` itself — that responsibility stays in the entity.

### `AudioEntity.Localization.cs` — `PickNewClip()` Case

```
case Localization:
  1. EnsureClipSelectionStrategy<LocalizationClipStrategy>()
  2. strategy.SelectClip(Clips, ...) → finds BroAudioClip matching current locale
  3. If null → warning already logged by strategy, return null (AudioPlayer skips playback)
  4. Resolve AudioClip:
         LocalizationSettings.AssetDatabase
             .GetLocalizedAssetAsync<AudioClip>(LocalizationTable, LocalizationEntry)
             .WaitForCompletion()
  5. If resolved clip is null →
         log warning: "[BroAudio] No AudioClip set in table for locale '{locale}' on entity '{name}'."
         return null
  6. Return new LocalizedBroAudioClipWrapper(broAudioClip, resolvedAudioClip)
```

> **Future note:** Step 4 uses `WaitForCompletion()` for synchronicity, consistent with the Addressables pattern. This can cause a hitch on first play if the asset is not preloaded. A `BroAudio.PreloadLocalizationAssets()` API should be considered when revisiting this.

### Null Handling in `AudioPlayer`

Confirm that `AudioPlayer` already handles a null `IBroAudioClip` (or a null `AudioClip` within it) by skipping playback gracefully, consistent with how Addressables handles missing references.

---

## 3. Editor

### `ReorderableClips.Localization.cs` — New Partial Class (`#if PACKAGE_LOCALIZATION`)

When `CurrentPlayMode == Localization`:

#### Header Area

Replace the standard clips header with:
- **Asset Table picker** — `TableReference` field writing to `AudioEntity.LocalizationTable`.
- **Entry Key picker** — `TableEntryReference` field, populated as a dropdown from the selected table's keys, writing to `AudioEntity.LocalizationEntry`.

#### Each List Row (one row per configured locale)

| Column | Content |
|--------|---------|
| Locale label | Read-only. Derived from `BroAudioClip.Locale` (e.g., "English (en)"). |
| AudioClip field | Editable `ObjectField`. Shows the clip currently in the Asset Table for this locale + entry. Assignment writes through to the Asset Table via the Localization editor API — **not** to `BroAudioClip.AudioClip`. |
| Play preview button | Resolves clip from the table at editor time and previews it. |
| Volume slider | Stored in `BroAudioClip.Volume`. |
| Fade sliders | Stored in `BroAudioClip.FadeIn` / `FadeOut`. |

#### Add Row (`+` Button)

Opens a locale picker populated from **all locales defined in the project's Localization Settings** (not just those with clips in the table). Selecting a locale appends a new `BroAudioClip` with that `Locale` set. Does **not** automatically add anything to the Asset Table.

#### Remove Row (`-` Button)

Removes the `BroAudioClip` row from the entity. Does **not** remove anything from the Asset Table (non-destructive by design).

#### Editor Locale Refresh

Subscribe to `LocalizationSettings.SelectedLocaleChanged` (or Unity's equivalent editor event) so the displayed clips in the list refresh automatically when the editor locale changes.

### Library Manager — Entry Point for Localization Setup

In `LibraryManagerWindow` (guarded by `#if PACKAGE_LOCALIZATION`):
- A "Localization" play mode option is available when setting up a new entity.
- Converting an existing entity to Localization mode clears all `BroAudioClip.AudioClip` references (they become unused) and prompts the developer to assign the table and entry key.

---

## 4. Assembly Definition Changes

- Add `com.unity.localization` as an **optional** assembly reference in the relevant `.asmdef` files (both Runtime and Editor).
- Define scripting symbol `PACKAGE_LOCALIZATION` (consistent with the existing `PACKAGE_ADDRESSABLES` pattern).
- Document the minimum supported version of the Unity Localization package (1.x vs 2.x APIs differ in places).

---

## 5. New Files Summary

```
Assets/BroAudio/Runtime/
  Enums/MulticlipsPlayMode.cs                            ← add Localization value (append at end)
  DataStruct/BroAudioClip.Localization.cs                ← new partial class (+Locale field)
  DataStruct/Core/AudioEntity.Localization.cs            ← new partial class (table/entry fields + PickNewClip case)
  Utility/ClipSelection/LocalizationClipStrategy.cs      ← new IClipSelectionStrategy implementation
  Utility/LocalizedBroAudioClipWrapper.cs                ← new IBroAudioClip wrapper class

Assets/BroAudio/Editor/
  EntityPropertyDrawer/ReorderableClips.Localization.cs  ← new partial class (Localization-mode drawing)
```

---

## 6. Risks & Open Items

| Item | Detail |
|------|--------|
| **Async loading hitch** | `WaitForCompletion()` can stall on first play if the asset bundle is not preloaded. A `BroAudio.PreloadLocalizationAssets()` API is deferred but should be designed before a production release. |
| **Locale row vs table mismatch** | If a locale exists in `Clips[]` but has no clip in the table, step 5 of `PickNewClip()` catches it. If a locale is in the table but not in `Clips[]`, playback is skipped with a warning. Both paths are handled. |
| **Table/Entry not set** | If `LocalizationTable` or `LocalizationEntry` is unset, `PickNewClip()` should fail fast with a descriptive error rather than throwing an unhandled exception. |
| **Editor locale change refresh** | The clip list must subscribe to locale-change events; otherwise the displayed clips become stale when the developer switches preview locale. |
| **`ResetMultiClipStrategy()`** | Verify whether `LocalizationClipStrategy` needs special reset logic (e.g., if locale changes between consecutive plays). |
| **Sequence / Random + Localization** | Out of scope (single-entry only). If ever requested, entry-level selection and locale-level resolution would need to be composed. Worth a code comment at the Localization case in `PickNewClip()`. |
