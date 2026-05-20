# BroAudio Localization Workflow Improvement — Implementation Plan

## 1. Phases at a glance

| # | Phase | Depends on | Layer |
|---|-------|------------|-------|
| 1 | Unify `LoadAssetAsync` for Localization entities | — | Runtime |
| 2 | Expose unified locale-clip subscription per `SoundID` | — (shares helper with 1) | Runtime |
| 3 | Force `UseAddressables = true` when `PlayMode == Localization` | — | Editor |
| 4 | Route per-locale clip assignment through `AssetTableCollection.Add/RemoveAssetFromTable` | 3 (visual lock; not a hard dep) | Editor |

Recommended execution order: **1 → 3 → 4 → 2**. Phases 1+2 and 3+4 can run in parallel pairs.

**Out of scope:** runtime playback strategy changes; non-blocking `PlayAsync`; combining `Localization` with `Sequence`/`Random` per-entry resolution.

---

## 2. Current State (anchors)

| File | Anchor | Role |
|------|--------|------|
| `Runtime/BroAudio.cs:306-322` | `PreloadLocalizationAssets` / `ReleaseLocalizationPreload` | Localization public surface today |
| `Runtime/BroAudio.cs:324-372` | `IsLoaded`, `LoadAllAssetsAsync`, `LoadAssetAsync`, `ReleaseAllAssets`, `ReleaseAsset` | Currently `#if PACKAGE_ADDRESSABLES` only |
| `Runtime/SoundManager/SoundManager.Addressables.cs:53-63` | `LoadAssetAsync(SoundID,int)` | Addressables-only implementation |
| `Runtime/SoundManager/SoundManager.Addressables.cs:81-94` | `TryGetAddressableEntity` | Logs error for non-addressable entities |
| `Runtime/SoundManager/SoundManager.Localization.cs:23-61` | `PreloadLocalizationAssets` | Already uses `LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>` |
| `Runtime/SoundManager/SoundManager.Localization.cs:89-109` | `OnSelectedLocaleChanged` | Releases stale handles and re-preloads |
| `Runtime/DataStruct/Core/AudioEntity.Addressables.cs:13` | `public bool UseAddressables = false;` | Per-entity flag |
| `Runtime/DataStruct/Core/AudioEntity.Localization.cs:9-13` | `_localizationTable`, `_localizationEntry` | Per-entity Localization refs |
| `Editor/EntityPropertyDrawer/AudioEntityEditor.cs:306-318` | `DrawUseAddressablesToggle` | No Localization awareness today |
| `Editor/EntityPropertyDrawer/ReorderableClips.cs:380-429` | `OnDrawHeader` | Play-mode dropdown + `CheckLocalizationMode` hook |
| `Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:57-90` | `CheckLocalizationMode` / `ConfirmSwitchToLocalizationMode` | Switch-into-Localization confirm flow |
| `Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:299-367` | `DrawLocalizationTableClipElement` | Per-locale row (ObjectField only, no drag) |
| `Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs:675-711` | `TryGetClipFromTable` / `TrySetClipInTable` | **Bug:** writes `entry.Guid` directly, bypassing `AssetTableCollection.AddAssetToTable` |

**Core defect today:** `TrySetClipInTable` mutates `entry.Guid` directly when an entry already exists. That bypasses `AssetTableCollection.AddAssetToTable`, which is the only API that (a) registers the asset in Addressables, (b) places it in the locale group resolved by Addressable Group Rules, (c) applies the `Locale_{code}` label, and (d) cleans up the previous asset's labels/group. Result: clips appear in the table but may be missing from Addressables, missing the locale label, or in the wrong group — breaking the Localization Tables analyzer.

All paths assume `PACKAGE_LOCALIZATION` is defined for code under `#if PACKAGE_LOCALIZATION`; in practice Localization pulls in Addressables, so the combined `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION` guard is defensive.

---

## 3. Phase 1 — Unify `LoadAssetAsync`

**Touches:** `Runtime/BroAudio.cs`, `Runtime/SoundManager/SoundManager.Addressables.cs`, `Runtime/SoundManager/SoundManager.Localization.cs`

**Intent:** Make `BroAudio.LoadAssetAsync(SoundID[, int])` resolve Localization-mode entities through Unity Localization, while leaving the Addressables path byte-identical. Return type stays `AsyncOperationHandle<AudioClip>` — Unity's `GetLocalizedAssetAsync<AudioClip>` already returns it.

**Changes:**

1. In `BroAudio.cs`, move `LoadAssetAsync`, `LoadAllAssetsAsync`, `IsLoaded`, `ReleaseAsset`, `ReleaseAllAssets` from `#if PACKAGE_ADDRESSABLES` into the combined `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION` guard. No signature changes. Existing callers must continue to compile.
2. In `SoundManager.Addressables.cs`'s `LoadAssetAsync(SoundID, int)`, branch on `entity.PlayMode == MulticlipsPlayMode.Localization` **before** any addressable-only checks. Do not route Localization-mode entities through `TryGetAddressableEntity` (it logs spurious errors). Apply the same branch in `LoadAllAssetsAsync`.
3. In `SoundManager.Localization.cs`, add `private AsyncOperationHandle<AudioClip> LoadLocalizedAssetAsync(SoundID, AudioEntity)`. Validate `LocalizationTable`/`LocalizationEntry` (empty → warning + `default`). Resolution: `LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>(table, entry)`. Refactor `PreloadLocalizationAssets` to share this validation helper.
4. For `LoadAllAssetsAsync` in Localization mode, wrap the single resolved clip into an `IList<AudioClip>` via `Addressables.ResourceManager.CreateChainOperation` so the return type stays consistent with the Addressables path.
5. XML doc on `LoadAssetAsync(SoundID, int)` must note: *for Localization-mode entities, `clipIndex` is ignored; one entry resolves to one clip per active locale.*

**Out of scope:** deprecating/warning on the `clipIndex` overload; non-blocking `PlayAsync`.

**Done when:**

- Project compiles with `PACKAGE_LOCALIZATION` defined and undefined.
- Addressable entities behave identically (same code path, no extra branches taken).
- Localization entity with valid table+entry: `LoadAssetAsync(id).WaitForCompletion()` returns the locale-correct clip.
- Localization entity with empty table or entry: returns `default`, single warning logged.

---

## 4. Phase 2 — Localized Clip Change Subscription

**Touches:** `Runtime/BroAudio.cs`, `Runtime/SoundManager/SoundManager.Localization.cs`

**Intent:** Unify three concerns under a single subscription per `SoundID`: (a) implicit load on first subscribe — `LocalizedAsset<T>.AssetChanged`'s first attach internally triggers `LoadAssetAsync` via `ForceUpdate` → `HandleLocaleChange`, so callers do **not** need to pair this with `BroAudio.LoadAssetAsync`; (b) fire when the asset is ready, or immediately with the cached value if the load is already `IsDone`; (c) fire again on every locale-driven change — `LocalizedAsset<T>` already subscribes to `LocalizationSettings.SelectedLocaleChanged` internally and re-invokes all handlers, so BroAudio does not roll its own `SelectedLocaleChanged` listener. Payload is `SoundID`, **not** `AudioClip`, to match BroAudio's playback contract (there is no `Play(AudioClip)` overload — playback is `SoundID`-driven) and to mirror the existing `IAudioPlayer.OnEnd(Action<SoundID>)` precedent. Semantically equivalent to `LocalizedAsset<TObject>.AssetChanged` but stays inside BroAudio's `SoundID` vocabulary.

**Public surface (under `#if PACKAGE_LOCALIZATION`):**

```csharp
public static void SubscribeLocalizedClipChanged(SoundID id, Action<SoundID> handler);
public static void UnsubscribeLocalizedClipChanged(SoundID id, Action<SoundID> handler);
```

**Changes:**

1. In `SoundManager.Localization.cs`, add `Dictionary<SoundID, LocalizedAsset<AudioClip>>` storage. Lazy-init.
2. **Decision (committed):** maintain a `Dictionary<(SoundID, Action<SoundID>), LocalizedAsset<AudioClip>.ChangeHandler>` so `-=` of the caller's `Action<SoundID>` removes the matching lambda. The wrapper lambda reads `clip => handler(id)` — it discards the resolved `AudioClip` and forwards the `SoundID` instead. Subscribing the same `(id, handler)` twice is a no-op. This map is required — wrapping in a lambda without it breaks unsubscribe.
3. Validate the entity is in Localization mode on `Subscribe`; warn + bail otherwise.
4. **Handle release is delegated to Unity.** `LocalizedAsset<T>` releases its underlying Addressables handle automatically when the last `ChangeHandler` is removed (`ClearLoadingOperation` → `AddressablesInterface.Release`, `LocalizedAsset.cs:200-208`). Our wrapper's only cleanup responsibility on `Unsubscribe` is: (i) remove the `(id, handler)` entry from the lambda map, and (ii) when the last handler for an `id` detaches, remove the `LocalizedAsset<AudioClip>` from the storage dictionary so it can be GC'd. Do **not** call `Addressables.Release` on the underlying handle from BroAudio. On `ReleaseAllLocalizationPreloads` / `SoundManager.OnDestroy`, detach all handlers and clear the dictionary; Unity will release the handles as the last detach happens for each id.
5. XML docs on `BroAudio.SubscribeLocalizedClipChanged` must spell out the full lifecycle so callers know they don't need to pair it with `LoadAssetAsync` or `SelectedLocaleChanged`: implicit load on first subscribe, callback when ready (or synchronously if cached), callback on every locale change, automatic handle release when the last subscriber for the id detaches.

**Cross-feature:** when `LoadAssetAsync` has already been called for the same `SoundID`, `LocalizedAsset<AudioClip>` resolves synchronously and the handler fires immediately on subscribe. No extra wiring needed.

**Out of scope:** auto-unsubscribe on scene unload; `IDisposable` wrapper types.

**Done when:**

- Subscribe before any load → handler fires once with the `SoundID` after the asset resolves.
- Subscribe after a previous load on the same `SoundID` → handler fires synchronously with the `SoundID`.
- Switch `LocalizationSettings.SelectedLocale` → handler fires again with the `SoundID`.
- Unsubscribe → no further invocations (verify the lambda-map dispatch).
- Unsubscribing the last handler for a `SoundID` releases the underlying Addressables handle (Unity's `LocalizedAsset<T>` does this internally; our test verifies the storage-dictionary entry is gone).
- Second subscriber on the same `SoundID` fires immediately with the cached value.
- Subscribe on a non-Localization entity → warning logged, no subscription registered.

---

## 5. Phase 3 — Force `UseAddressables` in Localization Mode

**Touches:** `Editor/EntityPropertyDrawer/AudioEntityEditor.cs`, `Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs`

**Intent:** Whenever `PlayMode == Localization`, the per-entity `Addressables` toggle is locked **on** and rendered non-interactive. The serialized `UseAddressables` field is unchanged; we only gate user input.

**Changes:**

1. In `DrawUseAddressablesToggle` (`AudioEntityEditor.cs:306-318`), wrap only the `#if PACKAGE_LOCALIZATION` Localization-aware branch around the toggle. When `playMode == Localization`:
   - Self-heal: if `useAddressablesProp.boolValue` is false, set to true.
   - Render inside `using (new EditorGUI.DisabledScope(true))`.
   - Use a `GUIContent` with tooltip *"Locked on while in Localization mode — Unity Localization requires Addressables."*
   - **Do not delete or restructure the non-Localization code path.** Keep the existing `ToggleLeft` + `SwitchAddressable` flow unchanged when not in Localization mode.
2. In `ConfirmSwitchToLocalizationMode` (`ReorderableClips.Localization.cs:57-90`):
   - Update the confirm dialog body to mention that Addressables will be force-enabled.
   - After `ResetBroAudioClipSerializedProperties`, set the `UseAddressables` property to `true` and call `ApplyModifiedProperties`.
3. **Do not** modify the asset-level `isAddressable` toggle in `AudioEntityEditor.AdditionalProperties.cs` (`DrawEntityAddressableProperty`). That field governs the `AudioAsset` ScriptableObject's own Addressables registration, which is orthogonal.

**Out of scope:** auto-revert `UseAddressables` when leaving Localization mode (intentional — leave it as-is).

**Done when:**

- New entity → switch to Localization → toggle becomes greyed out, `UseAddressables` is true.
- Direct-reference entity → switch to Localization → confirm dialog → clips cleared, `UseAddressables = true`, toggle greyed out.
- Switch back to a non-Localization mode → toggle becomes interactive again, retains true (no auto-revert).
- Inspector tooltip on the greyed toggle matches the locked-state copy.

---

## 6. Phase 4 — Drag-and-Drop via `AddAssetToTable`

**Touches:** `Editor/EntityPropertyDrawer/ReorderableClips.Localization.cs`

**Intent:** Per-locale row assignment in BroAudio (drag-drop or ObjectField) must produce the same Addressables registration, group placement, label, and table-entry GUID that dragging into the Localization Tables window produces. Single source of truth: `AssetTableCollection.AddAssetToTable` + `RemoveAssetFromTable`. Do **not** roll custom group/label logic.

**Changes:**

1. Rewrite `TrySetClipInTable` (`ReorderableClips.Localization.cs:691-711`):
   - Resolve `tableCollection`, `table`, `entryKey` via the existing helper. Bail silently if any are missing.
   - `Undo.RecordObject(table, "Set Localized Audio Clip")`.
   - If the entry currently has a GUID: `tableCollection.RemoveAssetFromTable(table, entryKey, createUndo: true)`. Always pair with the add — don't short-circuit on "same clip", the call is idempotent and cleanup is the point.
   - If `clip != null`: `tableCollection.AddAssetToTable(table, entryKey, clip, createUndo: true)`.
   - `EditorUtility.SetDirty(table)` + `EditorUtility.SetDirty(tableCollection.SharedData)` + `AssetDatabase.SaveAssets()`.
2. In `DrawLocalizationTableClipElement` (`ReorderableClips.Localization.cs:299-367`), add a drag-and-drop handler scoped to the row's `clipRect`. On `DragUpdated`/`DragPerform` with an `AudioClip` in `DragAndDrop.objectReferences`, set `visualMode = Copy` and on perform call `AcceptDrag` + `TrySetClipInTable(localeCode, dragged)` + `evt.Use()`. The existing `ObjectField` change-check stays as a parallel entry point — both route through `TrySetClipInTable`.
3. Localization rows are already `draggable: false` (`ReorderableClips.Localization.cs:99`); no conflict with `ReorderableList` row reordering.

**Out of scope:** dragging onto the list header to broadcast to all locales; multi-clip round-robin drops.

**Edge cases (must hold):**

- Non-`AudioClip` drag: `visualMode` stays `Rejected`, no-op.
- Drag onto a row whose entry key is empty: helper returns false, silent no-op.
- ObjectField cleared to `null`: `RemoveAssetFromTable` only.
- Same-clip re-drag: harmless (idempotent add).

**Done when:**

- Manual scenario passes: assign clip via BroAudio → Localization Tables window shows the same clip on the same row → Addressables window shows it in the resolved Locale group with `Locale_{code}` label → Addressables Analyzer reports no Localization issues.
- Reassigning a row removes the previous asset's `Locale_{code}` label (unless another locale still references it).
- Clearing a row to null removes the entry's GUID and label.

---

## 7. Risks (only the actionable ones)

| Risk | Mitigation |
|------|------------|
| `LocalizedAsset<T>.AssetChanged` lambda-wrapping breaks unsubscribe | Phase 2 commits to the `(SoundID, Action<SoundID>) → ChangeHandler` map. Do not skip it. |
| `AddAssetToTable` requires the dragged asset to be on disk | All drag sources in the Inspector are AssetDatabase objects. If a non-asset path is ever introduced, gate with `AssetDatabase.GetAssetPath != ""`. |
| Forcing `UseAddressables = true` surprises code-driven entity setup | Toggle's locked tooltip explains it; the entity won't play correctly if Addressables is off and Localization is on, so silent failure is not introduced. |

Informational caveats (`SharedData` dirtying, AssetDatabase sync cost, etc.) are inlined in the relevant phase and not duplicated here.

---

## 8. References

- Unity Localization Addressables integration: <https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Addressables.html>
- `AssetTableCollection.AddAssetToTable`: <https://docs.unity3d.com/Packages/com.unity.localization@1.5/api/UnityEditor.Localization.AssetTableCollection.AddAssetToTable.html>
- `LocalizedAsset<TObject>.AssetChanged`: <https://docs.unity3d.com/Packages/com.unity.localization@1.4/api/UnityEngine.Localization.LocalizedAsset-1.html>
- Status snapshot: `Docs/localization-feature-status.md`
- Prior architecture spec: `Docs/unity-localization-plan.md`
