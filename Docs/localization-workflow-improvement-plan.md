# BroAudio Localization Workflow Improvement — Implementation Plan

> **Branch**: `claude/improve-localization-workflow-LOPh1`
> **Baseline**: existing Localization feature on this branch (see `Docs/localization-feature-status.md`)
> **Generated**: 2026-05-19

---

## 1. Goals

Refine the existing Unity Localization integration so that it composes cleanly with the existing Addressables pipeline, mirrors the Unity Localization Tables workflow inside BroAudio, and removes footguns that come from the two systems being treated independently.

Four concrete requirements:

1. **Unify the public `LoadAssetAsync` API.** `BroAudio.LoadAssetAsync(SoundID, [int])` is currently `#if PACKAGE_ADDRESSABLES` only. Make the same call work for entities in `MulticlipsPlayMode.Localization` by branching inside `SoundManager.LoadAssetAsync`. The existing Addressables path remains byte-identical.
2. **Wire the Unity Localization `AssetChanged` workflow.** Expose a way for game code to subscribe to clip changes per `SoundID`, matching the semantics of `LocalizedAsset<TObject>.AssetChanged` (fires on first subscribe and on locale/entry changes).
3. **Force `UseAddressables = true` in Localization mode.** Addressables is a hard dependency of Unity Localization; the entity's `Addressable` toggle must be locked on (and visually disabled) whenever `PlayMode == Localization`.
4. **Make per-locale clip assignment go through the proper Asset Table workflow.** Drag-and-drop / ObjectField assignment in `ReorderableClips.Localization` must add/remove the asset through `AssetTableCollection.AddAssetToTable` / `RemoveAssetFromTable` so the Locale group, the `Locale_{code}` label, and Addressables registration are all set up exactly the way the Unity Localization Tables window would set them.

Out of scope: changing the runtime playback strategy (`LocalizationClipStrategy`), adding a non-blocking `PlayAsync`, or supporting `MulticlipsPlayMode.Localization` combined with Sequence/Random per-entry resolution.

---

## 2. Current State Snapshot

Anchors used by this plan (file:line):

| File | Anchor |
|------|--------|
| `Assets/BroAudio/Runtime/BroAudio.cs:306-322` | `PreloadLocalizationAssets` / `ReleaseLocalizationPreload` — Localization public surface today. |
| `Assets/BroAudio/Runtime/BroAudio.cs:324-372` | `IsLoaded`, `LoadAllAssetsAsync`, `LoadAssetAsync`, `ReleaseAllAssets`, `ReleaseAsset` — all `#if PACKAGE_ADDRESSABLES`. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Addressables.cs:53-63` | `LoadAssetAsync(SoundID, int)` — current Addressables-only implementation. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Addressables.cs:81-94` | `TryGetAddressableEntity` — logs an error if entity isn't addressable. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Localization.cs:23-61` | `PreloadLocalizationAssets` — already uses `LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>`. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Localization.cs:89-109` | `OnSelectedLocaleChanged` — releases stale handles and re-preloads. |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Addressables.cs:13` | `public bool UseAddressables = false;` |
| `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs:9-13` | `_localizationTable`, `_localizationEntry` fields. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs:306-318` | `DrawUseAddressablesToggle` — Addressables checkbox, no Localization awareness today. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.AdditionalProperties.cs:89-138` | `DrawEntityAddressableProperty` — the per-entity-asset Addressables setup. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.cs:380-429` | `OnDrawHeader` — the play-mode dropdown and `CheckLocalizationMode` hook. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:299-367` | `DrawLocalizationTableClipElement` — the per-locale row (ObjectField, no drag handling). |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:675-711` | `TryGetClipFromTable` / `TrySetClipInTable` — current write path, **does not** use `AddAssetToTable` when the entry already exists. |
| `Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs:43-78` | Playback-time resolution; the warning case for "no preload" is the main perf risk. |

The current Localization write path (`TrySetClipInTable`) is the single most important bug to fix. When an `AssetTableEntry` already exists, the method writes `entry.Guid = guid` directly. That bypasses `AssetTableCollection.AddAssetToTable`, which is the only API that:

- Adds the asset to the Addressables group selected by the project's Addressable Group Rules.
- Tags the asset with the `Locale_{code}` label.
- Cleans up the previous asset's labels/group if the entry already pointed at something else.

The result today: clips dragged in BroAudio show up in the table at runtime but may be missing from Addressables, missing the locale label, or stuck in the wrong group — which breaks the analyzer fixers in the Localization Tables window.

---

## 3. High-Level Architecture

```
                ┌───────────────────────────┐
                │   BroAudio (public API)   │
                └─────────────┬─────────────┘
                              │
              LoadAssetAsync(SoundID, [clipIndex])
                              │
                              ▼
               ┌────────────────────────────────────┐
               │   SoundManager.LoadAssetAsync      │
               │                                    │
               │  if entity.PlayMode == Localization│
               │       → LoadLocalizedAssetAsync    │
               │  else if entity.UseAddressables    │
               │       → existing addressable path  │
               └─────────────┬──────────────────────┘
                             │
            ┌────────────────┴─────────────────┐
            ▼                                  ▼
  GetLocalizedAssetAsync<AudioClip>     AssetReferenceT<AudioClip>
  (LocalizationSettings.AssetDatabase)   .LoadAssetAsync()
            │                                  │
            └──────────  AsyncOperationHandle<AudioClip>  ──────────┘
                                  │
                                  ▼
                       returned to game code
```

Editor side:

```
┌──────────────────────────────────────────────┐
│ AudioEntityEditor.DrawUseAddressablesToggle  │
│  if PlayMode == Localization                 │
│     ToggleLeft is disabled, value forced true│
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ ReorderableClips.Localization                │
│   per-locale row:                            │
│     ObjectField              ─────┐          │
│     drag-and-drop into row   ─────┤──► AssignClipToLocale(locale, clip)
│                                   │          │
│  AssignClipToLocale:              │          │
│   • old != null && new != old →   ▼          │
│     RemoveAssetFromTable(oldClip)            │
│   • new != null →                            │
│     AddAssetToTable(table, entryKey, new)    │
│   • new == null && old != null →             │
│     RemoveAssetFromTable(oldClip)            │
└──────────────────────────────────────────────┘
```

---

## 4. Phase 1 — Unify `LoadAssetAsync`

### 4.1 Goal

Make `BroAudio.LoadAssetAsync(SoundID id)` and `BroAudio.LoadAssetAsync(SoundID id, int clipIndex)` work for Localization entities **without changing the Addressables path**. The return type stays `AsyncOperationHandle<AudioClip>` (Unity Localization's `GetLocalizedAssetAsync<AudioClip>` already returns this type).

### 4.2 Public API

**`Assets/BroAudio/Runtime/BroAudio.cs`**

- Move the existing `LoadAssetAsync`, `LoadAllAssetsAsync`, `ReleaseAsset`, `ReleaseAllAssets`, `IsLoaded` methods out of `#if PACKAGE_ADDRESSABLES` and into a combined `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION` block. (`using UnityEngine.ResourceManagement.AsyncOperations;` at the top of the file is already guarded by the same combined symbol — `BroAudio.cs:7-9`.)
- No new method signatures. Existing callers continue to compile.
- For an entity in Localization mode, `clipIndex` is **ignored** (one entity = one entry = one resolved clip per active locale). Document this in the XML doc comment with a note: *"For Localization-mode entities, the clip resolves from the entity's `LocalizationTable` + `LocalizationEntry` for the current locale; `clipIndex` is ignored."*
- `LoadAllAssetsAsync` for a Localization entity returns a handle that resolves to a single-element `IList<AudioClip>` (the current-locale clip wrapped). Reuse `CreateCompletedOperation` via `Addressables.ResourceManager` so the return type stays consistent.

### 4.3 SoundManager dispatch

**`Assets/BroAudio/Runtime/SoundManager/SoundManager.Addressables.cs:53-63`** — rename the file or keep it; behavior change only:

```csharp
public AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex)
{
    if (!TryGetEntity(id, out var iEntity) || !(iEntity is AudioEntity entity))
    {
        return default;
    }

#if PACKAGE_LOCALIZATION
    if (entity.PlayMode == MulticlipsPlayMode.Localization)
    {
        return LoadLocalizedAssetAsync(id, entity);
    }
#endif

    // existing path
    if (!entity.UseAddressables)
    {
        Debug.LogError($"The entity {id.ToString().ToBold()} isn't marked as addressable…");
        return default;
    }
    if (clipIndex < 0 || clipIndex >= entity.Clips.Length) return default;
    var clip = entity.Clips[clipIndex];
    var result = clip.LoadAssetAsync();
    UpdateLoadedEntityLastPlayedTime(id);
    return result;
}
```

`TryGetAddressableEntity` currently logs an error for non-addressable entities; that helper is still useful for the pure-Addressables APIs but **must not** be called in the dispatch above when Localization mode is in play — Localization entities are not flagged `UseAddressables` from the user's POV (Phase 3 changes that, but the LoadAssetAsync code must not rely on the flag for routing).

### 4.4 Localization loader

**`Assets/BroAudio/Runtime/SoundManager/SoundManager.Localization.cs`** — add a private helper that mirrors what `PreloadLocalizationAssets` does internally, but without the preload-cache bookkeeping:

```csharp
private AsyncOperationHandle<AudioClip> LoadLocalizedAssetAsync(SoundID id, AudioEntity entity)
{
    if (entity.LocalizationTable.ReferenceType == TableReference.Type.Empty ||
        entity.LocalizationEntry.ReferenceType == TableEntryReference.Type.Empty)
    {
        Debug.LogWarning(Utility.LogTitle +
            $"[{nameof(LoadAssetAsync)}] LocalizationTable or LocalizationEntry is not set on entity '{entity.Name}'.");
        return default;
    }

    return LocalizationSettings.AssetDatabase
        .GetLocalizedAssetAsync<AudioClip>(entity.LocalizationTable, entity.LocalizationEntry);
}
```

Refactor `PreloadLocalizationAssets` to call this helper so we don't duplicate the validation block.

`LoadAllAssetsAsync` for Localization mode: build a single-element list using `Addressables.ResourceManager.CreateChainOperation` on the handle above.

### 4.5 Caller compatibility

- Existing callers that wrap `BroAudio.LoadAssetAsync(...)` in `#if PACKAGE_ADDRESSABLES` will still compile (the symbol is still defined when Addressables is present). The new combined guard means the same method also exists when only Localization is installed — but in practice Localization pulls in Addressables, so the combined symbol is mainly defensive.
- Consider deprecating the per-clip-index overload for Localization in a future pass; for now, keep silent (no warning log) so calls from existing demo code don't spam.

### 4.6 Tests / Validation

- Localization entity with valid table+entry: `LoadAssetAsync(id)` returns a non-default handle, `WaitForCompletion()` returns the locale-correct clip.
- Localization entity with empty table or empty entry: returns `default`, warning logged once.
- Addressable entity: byte-identical behavior to before (no regression).
- Non-addressable, non-Localization entity: same error message as before.

---

## 5. Phase 2 — Localization `AssetChanged` Workflow

### 5.1 Goal

Game code should be able to react to "the clip for this `SoundID` just changed because the player switched locale (or because a designer edited the table at runtime)." This mirrors `LocalizedAsset<TObject>.AssetChanged` — the event fires on first subscribe (with the current value) and on every subsequent change.

### 5.2 Public API

Add to **`Assets/BroAudio/Runtime/BroAudio.cs`** under `#if PACKAGE_LOCALIZATION`:

```csharp
/// <summary>
/// Subscribes to per-SoundID locale-asset change events. The handler fires
/// immediately with the current resolved clip when added, then again whenever
/// the SelectedLocale or the entity's table entry changes the resolved clip.
/// </summary>
public static void SubscribeAssetChanged(SoundID id, Action<AudioClip> handler)
    => SoundManager.Instance.SubscribeLocalizedAssetChanged(id, handler);

public static void UnsubscribeAssetChanged(SoundID id, Action<AudioClip> handler)
    => SoundManager.Instance.UnsubscribeLocalizedAssetChanged(id, handler);
```

Semantics match Unity's `LocalizedAsset<TObject>.AssetChanged` exactly: first subscriber triggers an initial load; later subscribers get the already-cached value synchronously.

### 5.3 Implementation strategy

The simplest correct implementation is to use Unity's `LocalizedAssetReference` (or `LocalizedAudioClip` if available) per-`SoundID` and forward its event. Storage shape in `SoundManager.Localization.cs`:

```csharp
private Dictionary<SoundID, LocalizedAsset<AudioClip>> _localizedAssetsBySoundID;
```

`LocalizedAsset<AudioClip>` is constructible in code by setting `TableReference` and `TableEntryReference`. When the first handler is added, Unity's internal machinery loads the asset and invokes the handler with the result; subsequent locale changes refire it. **We do not need to subscribe to `SelectedLocaleChanged` ourselves for this feature** — `LocalizedAsset<T>` does it internally.

```csharp
public void SubscribeLocalizedAssetChanged(SoundID id, Action<AudioClip> handler)
{
    if (!TryGetEntity(id, out var iEntity) || !(iEntity is AudioEntity entity) ||
        entity.PlayMode != MulticlipsPlayMode.Localization)
    {
        Debug.LogWarning(Utility.LogTitle + $"SubscribeAssetChanged: '{id}' is not a Localization-mode entity.");
        return;
    }

    _localizedAssetsBySoundID ??= new Dictionary<SoundID, LocalizedAsset<AudioClip>>();
    if (!_localizedAssetsBySoundID.TryGetValue(id, out var localized))
    {
        localized = new LocalizedAsset<AudioClip>
        {
            TableReference = entity.LocalizationTable,
            TableEntryReference = entity.LocalizationEntry,
        };
        _localizedAssetsBySoundID[id] = localized;
    }
    localized.AssetChanged += handler.Invoke;
}

public void UnsubscribeLocalizedAssetChanged(SoundID id, Action<AudioClip> handler)
{
    if (_localizedAssetsBySoundID != null &&
        _localizedAssetsBySoundID.TryGetValue(id, out var localized))
    {
        localized.AssetChanged -= handler.Invoke;
        // Optional: drop the LocalizedAsset entry when no subscribers remain
        // (LocalizedAsset releases its handle internally on the last unsub).
    }
}
```

`LocalizedAsset<T>.AssetChanged` is an `event` of type `LocalizedAsset<T>.ChangeHandler` which takes `(T value)`; converting from `Action<AudioClip>` works via lambda wrapping. Keep a `Dictionary<Action<AudioClip>, LocalizedAsset<AudioClip>.ChangeHandler>` if we need to support `-=` of the original Action.

### 5.4 Lifecycle

- On `SoundManager.OnDestroy` / `ReleaseAllLocalizationPreloads`, clear `_localizedAssetsBySoundID` after unsubscribing all handlers — `LocalizedAsset<T>` releases its Addressables handle when its last subscriber is removed.
- This is **independent** of `PreloadLocalizationAssets` — the two systems are layered (preload warms the Addressables cache; AssetChanged is a separate notification stream).

### 5.5 Cross-feature interaction

When `Preload` is also active, the `LocalizedAsset<AudioClip>` returned by Unity will see the warm Addressables cache and resolve synchronously. No new code needed — this falls out for free.

### 5.6 Tests / Validation

- Subscribe before any locale switch → handler fires once with the current clip.
- Switch locale via `LocalizationSettings.SelectedLocale = ...` → handler fires again with the new clip.
- Unsubscribe → no further calls.
- Subscribe a second handler while the first is still active → second handler fires immediately with the cached value.
- Subscribe for a non-Localization entity → warning logged, no subscription set up.

---

## 6. Phase 3 — Force `UseAddressables` in Localization Mode

### 6.1 Goal

When `PlayMode == Localization`, the entity-level `Addressable` checkbox in the Inspector (the per-clip-list one drawn by `DrawUseAddressablesToggle`, **and** the per-asset Addressables toggle drawn by `DrawEntityAddressableProperty` for the SoundAsset itself) must be:

- Visually disabled (greyed out, non-interactive).
- Forced to `true`.
- Accompanied by a tooltip explaining the lock.

The runtime `UseAddressables` field stays as a serialized bool; we just stop letting the user untick it while in Localization mode.

### 6.2 Editor changes

**`Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs:306-318`** — `DrawUseAddressablesToggle`:

```csharp
private void DrawUseAddressablesToggle(Rect position, ReorderableClips clips)
{
    SerializedProperty useAddressablesProp = serializedObject.FindProperty(nameof(AudioEntity.UseAddressables));
    Rect rect = GetRectAndIterateLine(position);
    Rect toggleRect = new Rect(rect) { width = 100f, x = position.xMax - 100f };

#if PACKAGE_LOCALIZATION
    var playModeProp = serializedObject.FindProperty(AudioEntity.EditorPropertyName.MulticlipsPlayMode);
    bool isLocalization = playModeProp.enumValueIndex == (int)MulticlipsPlayMode.Localization;
    if (isLocalization && !useAddressablesProp.boolValue)
    {
        useAddressablesProp.boolValue = true; // self-heal: force-on
    }
    var content = new GUIContent("Addressables",
        isLocalization ? "Locked on while in Localization mode — Unity Localization requires Addressables." : null);
    using (new EditorGUI.DisabledScope(isLocalization))
    {
        EditorGUI.BeginChangeCheck();
        useAddressablesProp.boolValue = EditorGUI.ToggleLeft(toggleRect, content, useAddressablesProp.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            EditorAudioPreviewer.Instance.StopAllClips();
            SwitchAddressable(useAddressablesProp, clips);
        }
    }
#else
    // existing code unchanged
#endif
}
```

`EditorGUI.DisabledScope(true)` greys the control out and discards input — no extra "the checkbox is reflecting the wrong state" path.

**Per-asset Addressables toggle** — `AudioEntityEditor.AdditionalProperties.cs:89-138` (`DrawEntityAddressableProperty`):

- The asset-level `isAddressable` checkbox (line 121) controls whether the `AudioAsset` (the SoundAsset ScriptableObject) is itself registered in Addressables. This is a separate concern from per-clip Addressables and is **not** automatically required by Localization. Leave it alone in Phase 3 — flag it as a follow-up only if users report confusion.

### 6.3 Switching INTO Localization mode

`ReorderableClips.Localization.cs:57-90` (`CheckLocalizationMode` / `ConfirmSwitchToLocalizationMode`) already clears stale `AudioClip` references on the switch. Extend the confirm path:

```csharp
private bool ConfirmSwitchToLocalizationMode()
{
    bool confirmed = EditorUtility.DisplayDialog(
        "Switch to Localization Mode",
        "Switching to Localization mode will clear all AudioClip references and clip properties on this entity, " +
        "and will force-enable Addressables on this entity (required by Unity Localization). Continue?",
        "Yes", "No");

    if (confirmed)
    {
        var clipsProp = _reorderableList.serializedProperty;
        for (int i = 0; i < clipsProp.arraySize; i++)
        {
            ResetBroAudioClipSerializedProperties(clipsProp.GetArrayElementAtIndex(i));
        }
        // NEW: force UseAddressables = true
        var useAddrProp = _entity.FindProperty(nameof(AudioEntity.UseAddressables));
        if (useAddrProp != null)
        {
            useAddrProp.boolValue = true;
        }
        clipsProp.serializedObject.ApplyModifiedProperties();
    }
    return confirmed;
}
```

### 6.4 Tests / Validation

- New entity → switch to Localization mode → `UseAddressables` flips to true and the toggle greys out.
- Existing addressable entity → switch to Localization → toggle stays true, greyed out.
- Existing direct-reference entity → switch to Localization → confirm dialog → clips cleared, `UseAddressables` flipped to true, toggle greyed out.
- Switch back from Localization to any other mode → toggle becomes interactive again, retains its true value (no auto-revert).

---

## 7. Phase 4 — Drag-and-Drop Matches Localization Tables Workflow

### 7.1 Goal

When the developer drops an `AudioClip` onto a per-locale row in the BroAudio Localization clip list (or assigns one through the ObjectField), BroAudio must perform the **same setup** that the Unity Localization Tables window would perform when the developer drops the clip into the equivalent cell:

1. Add the asset to Addressables (if not already there).
2. Place it in the Locale group selected by the project's **Addressable Group Rules** asset — typically `Localization-Assets-{LocaleName}`, or `Localization-Assets-Shared` if the same asset is used by multiple locales.
3. Tag it with the `Locale_{Code}` label.
4. Update the `AssetTable` entry to point at the new GUID.
5. If a previous asset was assigned to the same cell, clean up its label / group registration cleanly (so leftover labels don't keep the old asset alive in the wrong group).

The single API that does all of this for us is `AssetTableCollection.AddAssetToTable(AssetTable, TableEntryReference, Object, bool createUndo = false)`, paired with `AssetTableCollection.RemoveAssetFromTable(AssetTable, TableEntryReference, bool createUndo = false)`. Both internally consult the project's Addressable Group Rules — we should not roll our own group/label assignment logic.

### 7.2 Rewrite `TrySetClipInTable`

**`Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:691-711`**

Replace the current body:

```csharp
private void TrySetClipInTable(string localeCode, AudioClip clip)
{
    if (!TryGetAssetTableAndEntry(localeCode, out var tableCollection, out var table, out string entryKey))
    {
        return;
    }

    var entry = table.GetEntry(entryKey);
    bool hadOldAsset = entry != null && !string.IsNullOrEmpty(entry.Guid);

    Undo.RecordObject(table, "Set Localized Audio Clip");

    if (hadOldAsset)
    {
        // Cleans up Addressables label/group for the previous asset
        tableCollection.RemoveAssetFromTable(table, entryKey, createUndo: true);
    }

    if (clip != null)
    {
        // Adds to Addressables, applies Locale_{code} label, places in resolved group
        tableCollection.AddAssetToTable(table, entryKey, clip, createUndo: true);
    }

    EditorUtility.SetDirty(table);
    EditorUtility.SetDirty(tableCollection.SharedData);
    AssetDatabase.SaveAssets();
}
```

Notes:

- Always pair `RemoveAssetFromTable` + `AddAssetToTable`. Don't try to detect "same clip" — the cost is negligible and the cleanup is the whole point.
- `createUndo: true` lets the user Ctrl-Z the assignment, matching Localization Tables window behavior.
- Both `SharedData` and the table itself need to be marked dirty — the table holds the entry's GUID, the shared data holds the entry key index.

### 7.3 Add per-row drag-and-drop

**`Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:299-367`** — `DrawLocalizationTableClipElement`

Today the row exposes only an `ObjectField`. Add a drag-and-drop handler scoped to the row's `clipRect`:

```csharp
HandleLocalizationRowDragAndDrop(clipRect, localeCode);

EditorGUI.BeginChangeCheck();
var newClip = EditorGUI.ObjectField(clipRect, currentClip, typeof(AudioClip), false) as AudioClip;
if (EditorGUI.EndChangeCheck())
{
    TrySetClipInTable(localeCode, newClip);
}
```

And the helper:

```csharp
private void HandleLocalizationRowDragAndDrop(Rect rowRect, string localeCode)
{
    var evt = Event.current;
    if (!rowRect.Contains(evt.mousePosition)) return;
    if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

    AudioClip dragged = null;
    foreach (var obj in DragAndDrop.objectReferences)
    {
        if (obj is AudioClip ac) { dragged = ac; break; }
    }
    if (dragged == null) return;

    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
    if (evt.type == EventType.DragPerform)
    {
        DragAndDrop.AcceptDrag();
        TrySetClipInTable(localeCode, dragged);
        evt.Use();
    }
}
```

This routes drag-drops through the same `TrySetClipInTable` as the ObjectField — single source of truth.

### 7.4 Multi-drop ergonomics (optional, scope-controlled)

Out of scope for this iteration: dragging an AudioClip onto the *header* of the Localization list and auto-assigning it to every locale, or dragging multiple clips at once and round-robining them across locales. These are easy to add later but blur the per-locale-row semantics; defer.

### 7.5 Edge cases

| Case | Behavior |
|------|----------|
| Drag a non-AudioClip object | `DragAndDrop.visualMode` stays `Rejected`; nothing happens. |
| Drag onto a row whose locale has no table entry yet (table set but entry key empty) | `TryGetAssetTableAndEntry` returns false; silent no-op. Same as today. |
| Drag the same clip onto a row that already has it | Remove + re-add is harmless; the entry GUID stays the same and Addressables registration is idempotent. |
| Set ObjectField to `null` | `TrySetClipInTable(code, null)` triggers `RemoveAssetFromTable` only — entry is cleared, Addressables registration for that locale is removed, label cleaned up. |
| Table or entry not selected | `TryGetAssetTableAndEntry` already returns false; both drag and ObjectField paths short-circuit silently. |

### 7.6 Validation against the Localization Tables window

After the change, exercise this scenario manually:

1. Create a new entity in Localization mode, assign Table = `MyTable`, Entry = `entry_a`.
2. Drag `Clip_EN.wav` onto the English row in BroAudio.
3. Open **Window > Asset Management > Localization Tables**.
4. Inspect the `entry_a` row for the English locale — it must show `Clip_EN.wav`, and the Addressables window must list it in the resolved Locale group with the `Locale_en` label.
5. Repeat for Japanese with `Clip_JA.wav`.
6. Reassign English to `Clip_EN_v2.wav`. The Addressables registration for `Clip_EN.wav` should be removed (if no other locale uses it).
7. Open the Addressables Analyze window and run the analyzer; **no Localization-related issues** should be reported.

If step 7 surfaces issues, the implementation is wrong — `AddAssetToTable` / `RemoveAssetFromTable` are the only methods that keep the analyzer green.

---

## 8. File Manifest

### New files
*(none)*

### Modified files

| File | Phase | Change |
|------|-------|--------|
| `Assets/BroAudio/Runtime/BroAudio.cs` | 1, 2 | Move `LoadAssetAsync` family under combined `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION`. Add `SubscribeAssetChanged` / `UnsubscribeAssetChanged` under `#if PACKAGE_LOCALIZATION`. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Addressables.cs` | 1 | Add Localization branch in `LoadAssetAsync(SoundID, int)` and `LoadAllAssetsAsync`. Stop routing Localization calls through `TryGetAddressableEntity`. |
| `Assets/BroAudio/Runtime/SoundManager/SoundManager.Localization.cs` | 1, 2 | Add `LoadLocalizedAssetAsync`, refactor `PreloadLocalizationAssets` to share validation. Add `SubscribeLocalizedAssetChanged` / `UnsubscribeLocalizedAssetChanged` + `_localizedAssetsBySoundID` storage. Tear down on `ReleaseAllLocalizationPreloads`. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/AudioEntityEditor.cs` | 3 | `DrawUseAddressablesToggle` greys out + force-trues the checkbox in Localization mode. |
| `Assets/BroAudio/Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs` | 3, 4 | `ConfirmSwitchToLocalizationMode` forces `UseAddressables = true`. `DrawLocalizationTableClipElement` adds per-row drag-and-drop. `TrySetClipInTable` rewritten to use `RemoveAssetFromTable` + `AddAssetToTable`. |

### Files inspected but **not** modified

- `Assets/BroAudio/Runtime/DataStruct/Core/AudioEntity.Localization.cs` — schema unchanged.
- `Assets/BroAudio/Runtime/Utility/ClipSelection/LocalizationClipStrategy.cs` — playback path unchanged.
- `Assets/BroAudio/Runtime/Utility/LocalizedBroAudioClipWrapper.cs` — wrapper unchanged.
- `Assets/BroAudio/Runtime/Enums/MulticlipsPlayMode.cs` — enum unchanged.

---

## 9. Phase Order & Parallelization

```
Phase 1 (runtime: unify LoadAssetAsync)
Phase 2 (runtime: AssetChanged subscription)
       └─ depends on Phase 1 only via shared validation helper; otherwise parallel.

Phase 3 (editor: force UseAddressables)
Phase 4 (editor: drag-and-drop + AddAssetToTable)
       └─ Phase 3 and 4 are independent of Phase 1/2 and of each other.
```

Recommended order if implementing sequentially: 1 → 3 → 4 → 2.

- **1 first** because it unblocks Localization users from the public API (the most-asked-for surface).
- **3 next** because it prevents users from getting into the inconsistent "Localization on, Addressables off" state that Phase 4's drag-drop is otherwise affected by.
- **4** to fix the silent group/label corruption.
- **2** last — purely additive and doesn't touch any existing path.

---

## 10. Risks & Open Items

| Risk | Mitigation |
|------|------------|
| `LocalizedAsset<T>.AssetChanged` signature differs between Localization 1.0 and 1.5 | Both versions expose `event ChangeHandler` taking `(T value)` — adapter lambda is portable. Document minimum Localization version as 1.0 in `Docs/localization-feature-status.md` if not already. |
| `AddAssetToTable` requires the asset to be saved to disk (have an AssetDatabase path) before the call | All drag-drop sources in the editor are existing assets — they have a path. Document this in a comment if non-asset paths ever arise. |
| `RemoveAssetFromTable` when the entry is empty | Safe; the call is a no-op if the entry has no GUID. Still gated by the `hadOldAsset` check to avoid a redundant SetDirty. |
| Forcing `UseAddressables = true` may surprise users who set up a Localization entity through code | The toggle visually shows the locked state. Phase 3 also emits a tooltip explaining the constraint. No silent corruption — the entity simply won't play if Addressables is off and Localization is on. |
| Drag-drop in `DrawLocalizationTableClipElement` may race with `ReorderableList`'s own drag handling for row reordering | Localization rows are non-reorderable (`draggable: false` at `ReorderableClips.Localization.cs:99`). No conflict. |
| `AssetTableCollection.AddAssetToTable` modifies the AssetDatabase synchronously and can be slow on large projects | Acceptable — same cost the Localization Tables window pays. Avoid calling it from `OnGUI` paths that fire every frame (the drag-perform path is event-driven, single-shot). |
| `SubscribeAssetChanged` lifetime — handlers leak if game code forgets to unsubscribe | Same lifetime contract as Unity's `LocalizedAsset.AssetChanged`. Document the pattern in XML doc and the gitbook page (link from `BroAudio.cs` XML). |

---

## 11. Acceptance Checklist

| # | Requirement | Verification |
|---|-------------|--------------|
| 1 | `BroAudio.LoadAssetAsync(id)` works for Localization-mode entities. | Manual: switch entity to Localization, call `LoadAssetAsync(id).WaitForCompletion()`, assert clip is correct for `LocalizationSettings.SelectedLocale`. |
| 2 | `BroAudio.LoadAssetAsync(id)` still works for Addressable entities (no regression). | Manual: existing Addressables demo scene plays as before. |
| 3 | `BroAudio.SubscribeAssetChanged(id, handler)` invokes the handler on subscribe and on locale change. | Manual: subscribe, switch locale, verify two invocations with correct clips. |
| 4 | In Localization mode, the `Addressable` checkbox on the entity is greyed out and shows `true`. | Open the Inspector for a Localization-mode entity; confirm visual state and that clicking the toggle does nothing. |
| 5 | Switching an existing entity into Localization mode forces `UseAddressables = true`. | Toggle play mode through the dropdown; inspect the serialized object. |
| 6 | Dragging an AudioClip onto a per-locale row in the BroAudio Localization list creates the same Addressables registration, group, and label as dragging into the equivalent cell in the Localization Tables window. | Side-by-side comparison: run the scenario in §7.6. |
| 7 | Reassigning a locale row's clip removes the old asset's Localization registration cleanly (no orphaned labels). | Run the Addressables Analyzer after a reassignment — must report no Localization issues. |
| 8 | Clearing a locale row (setting ObjectField to null) calls `RemoveAssetFromTable` only. | Set to null, confirm in Localization Tables window that the entry is cleared and the previous clip no longer carries the `Locale_{code}` label (unless another locale still uses it). |

---

## 12. References

- Unity Localization — Addressables integration: <https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Addressables.html>
- `AssetTableCollection.AddAssetToTable`: <https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEditor.Localization.AssetTableCollection.AddAssetToTable.html>
- `AssetTableCollection.RemoveAssetFromTable`: <https://docs.unity3d.com/Packages/com.unity.localization@1.3/api/UnityEditor.Localization.AssetTableCollection.html>
- `LocalizedAsset<TObject>.AssetChanged`: <https://docs.unity3d.com/Packages/com.unity.localization@1.4/api/UnityEngine.Localization.LocalizedAsset-1.html>
- Existing BroAudio Localization status: `Docs/localization-feature-status.md`
- Existing BroAudio Localization architecture spec: `Docs/unity-localization-plan.md`
