# Plan: Localization Table Reference Into Clip List View

## Overview

Move the localization table/entry dropdowns from the `AudioEntityEditor` header area
into the `ReorderableClips` clip list itself. In Localization mode the list becomes:

- **Row 0** — Table reference row: the asset-table dropdown + entry dropdown
- **Row 1+** — One row per available locale (clips read directly from the asset table)

Additional constraints:
- The list must **not** be reorderable in Localization mode
- `AudioEntity.Clips` must remain **size 0** in Localization mode (all audio comes from
  the asset table at runtime)

---

## Files Changed

### 1. `ReorderableClips.Localization.cs` — Editor

**New fields**
```csharp
private List<int>        _localizationListData;   // dummy backing list
private ReorderableList  _localizationList;        // non-draggable list for Localization mode
```

**`InitLocalization`**
- Create `_localizationListData` as an empty `List<int>`.
- Create `_localizationList` via
  `new ReorderableList(_localizationListData, typeof(int), draggable: false, header: true, addButton: false, removeButton: false)`.
- Assign callbacks: `drawHeaderCallback`, `drawElementCallback`, `elementHeightCallback`.

**`UpdateLocalizationListCount()`** (called every repaint)
- Target count = 1 + `LocalizationSettings.AvailableLocales?.Locales?.Count ?? 0`.
- Resize `_localizationListData` to match target count (add/remove trailing elements).

**`DrawLocalizationHeader(rect)`** — minimal change
- Keep PlayMode enum popup (so user can switch away from Localization mode).
- Keep Master Volume slider.
- Remove "Locale" column label (no longer meaningful here).

**`OnDrawLocalizationListElement(rect, index, isActive, isFocused)`**
- `index == 0` → call `DrawLocalizationTableRow(rect)` (table + entry dropdowns).
- `index > 0`  → call `DrawLocalizationTableClipElement(rect, index - 1)`.

**`DrawLocalizationTableRow(rect)`**
- Replace the existing `DrawLocalizationTableDropdowns` public method.
- Layout: split rect horizontally, draw asset-table dropdown on left half, entry dropdown on right half.
- Logic unchanged from current `DrawAssetTableDropdown` / `DrawTableEntryDropdown`.

**`DrawLocalizationTableClipElement(rect, localeIndex)`** (replaces `DrawLocalizationElement`)
- Retrieve locale from `LocalizationSettings.AvailableLocales.Locales[localeIndex]`.
- Resolve `AudioClip` via `TryGetClipFromTable(locale.Identifier.Code)`.
- Draw: play-preview button | locale label | AudioClip object field (write back via `TrySetClipInTable`).
- **No volume slider** — no `BroAudioClip` entry exists; entity MasterVolume applies.

**`GetLocalizationElementHeight(index)`**
- Return `EditorGUIUtility.singleLineHeight + 4f` for all rows.

**`HasLocalizationTableClip`** (new bool property, optional helper)
- Returns true when table name and entry key are both non-empty; used by `HasClips`.

**`DrawLocalizationTableDropdowns(rect)`** — **remove or make no-op** (kept as empty stub
for the `#if PACKAGE_ADDRESSABLES` path in `AudioEntityEditor` if still referenced there,
otherwise fully removed).

---

### 2. `ReorderableClips.cs` — Editor

**`DrawReorderableList(Rect position)`**
```csharp
#if PACKAGE_LOCALIZATION
if (CurrentPlayMode == MulticlipsPlayMode.Localization)
{
    UpdateLocalizationListCount();
    _localizationList.DoList(position);
    return;
}
#endif
// existing path unchanged
```

**`Height` property**
```csharp
public float Height =>
#if PACKAGE_LOCALIZATION
    CurrentPlayMode == MulticlipsPlayMode.Localization
        ? _localizationList.GetHeight()
        :
#endif
    _reorderableList.GetHeight() + (HasHeaderMessage(out _) ? HeaderMessageHeight : 0f);
```

**`HasClips` property** — extend for Localization mode
```csharp
public bool HasClips =>
    (_reorderableList != null && _reorderableList.count > 0)
#if PACKAGE_LOCALIZATION
    || (CurrentPlayMode == MulticlipsPlayMode.Localization && HasLocalizationTableClip)
#endif
    ;
```

**`OnAdd` / `OnRemove` / `OnDrawHeader` / `OnDrawElement`** — no change needed
(Localization mode returns early before touching these).

---

### 3. `AudioEntityEditor.cs` — Editor

**`DrawGUI` — Tab.Clips block**
Remove the `#elif PACKAGE_LOCALIZATION` block that called
`data.Clips.DrawLocalizationTableDropdowns(...)` and adjusted `Offset`.
The dropdowns are now row 0 of the list itself.

```csharp
// BEFORE
#elif PACKAGE_LOCALIZATION
{
    var locPlayModeProp = ...;
    if ((MulticlipsPlayMode)locPlayModeProp.enumValueIndex == MulticlipsPlayMode.Localization)
    {
        Offset -= SingleLineSpace * 0.5f;
        data.Clips.DrawLocalizationTableDropdowns(GetRectAndIterateLine(position));
    }
}
#endif

// AFTER — block deleted entirely
```

**`GetClipListHeight`**
- Remove the `#elif PACKAGE_LOCALIZATION` height adjustment that accounted for the
  extra dropdown line (it is now included in `data.Clips.Height` automatically via
  `_localizationList.GetHeight()`).

---

### 4. `AudioEntity.Localization.cs` — Runtime

**`PickLocalizationClip`**

Current code calls `_clipSelectionStrategy.SelectClip(Clips, ...)` to find a `BroAudioClip`
matching the current locale. With `Clips` always empty this will always return null and emit
a warning. Fix:

```csharp
internal IBroAudioClip PickLocalizationClip(ClipSelectionContext context, out int index)
{
    // ... existing table/entry validation unchanged ...

    // Skip Clips-based strategy — Clips is intentionally empty in Localization mode.
    index = 0;

    var handle = LocalizationSettings.AssetDatabase
        .GetLocalizedAssetAsync<AudioClip>(_localizationTable, _localizationEntry);
    var resolvedClip = handle.WaitForCompletion();

    if (resolvedClip == null)
    {
        // ... existing warning log ...
        return null;
    }

    return new LocalizedBroAudioClipWrapper(resolvedClip);   // use default clip settings
}
```

---

### 5. `LocalizedBroAudioClipWrapper.cs` — Runtime

Add a second constructor that accepts only `AudioClip` and supplies defaults:

```csharp
public LocalizedBroAudioClipWrapper(AudioClip resolvedClip)
{
    _broAudioClip = null;
    _resolvedClip = resolvedClip;
}
```

Update each property to fall back to defaults when `_broAudioClip == null`:

```csharp
public float Volume       => _broAudioClip?.Volume       ?? AudioConstant.FullVolume;
public float Delay        => _broAudioClip?.Delay        ?? 0f;
public float StartPosition=> _broAudioClip?.StartPosition?? 0f;
public float EndPosition  => _broAudioClip?.EndPosition  ?? 0f;
public float FadeIn       => _broAudioClip?.FadeIn       ?? 0f;
public float FadeOut      => _broAudioClip?.FadeOut      ?? 0f;
```

---

## Data-Model Impact

| Field | Before | After |
|---|---|---|
| `AudioEntity.Clips[]` (Localization mode) | One entry per locale, each with `Locale` code | Always **empty** (size 0) |
| `AudioEntity._localizationTable` | Same | Same |
| `AudioEntity._localizationEntry` | Same | Same |

No new serialized fields are required. Existing assets that stored locale clips in
`AudioEntity.Clips` will simply have those entries ignored (they remain in the asset file
but are no longer used by the editor or runtime).

---

## Behaviour Summary

| Concern | Result |
|---|---|
| Reordering | Disabled (`draggable: false`) |
| Add / Remove buttons | Hidden (`displayAdd: false`, `displayRemove: false`) |
| Clips array size at runtime | 0 |
| Audio clip source at runtime | Unity Localization asset table |
| Per-locale volume | Removed; entity MasterVolume applies to all locales |
| Preview button | Works via `HasLocalizationTableClip` guard |
| Switching away from Localization mode | Clips array is still empty; user adds clips manually after switching |
