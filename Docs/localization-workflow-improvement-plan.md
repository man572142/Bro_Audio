# BroAudio Localization Workflow Improvement — Implementation Plan

## 1. Phases at a glance

| # | Phase | Depends on | Layer |
|---|-------|------------|-------|
| 1 | Unify `LoadAssetAsync` for Localization entities | — | Runtime |
| 2 | Expose `AssetChanged` subscription per `SoundID` | — (shares helper with 1) | Runtime |
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

## 4. Phase 2 — `AssetChanged` Subscription

**Touches:** `Runtime/BroAudio.cs`, `Runtime/SoundManager/SoundManager.Localization.cs`

**Intent:** Let game code subscribe to per-`SoundID` clip-change events that mirror `LocalizedAsset<TObject>.AssetChanged` semantics: fire on first subscribe with the current value, fire again on every locale/entry-driven change. Use Unity's `LocalizedAsset<AudioClip>` per `SoundID` and forward its event — do not roll a `SelectedLocaleChanged` listener in BroAudio.

**Public surface (under `#if PACKAGE_LOCALIZATION`):**

```csharp
public static void SubscribeAssetChanged(SoundID id, Action<AudioClip> handler);
public static void UnsubscribeAssetChanged(SoundID id, Action<AudioClip> handler);
```

**Changes:**

1. In `SoundManager.Localization.cs`, add `Dictionary<SoundID, LocalizedAsset<AudioClip>>` storage. Lazy-init.
2. **Decision (committed):** maintain a `Dictionary<(SoundID, Action<AudioClip>), LocalizedAsset<AudioClip>.ChangeHandler>` so `-=` of the caller's `Action<AudioClip>` removes the matching lambda. Subscribing the same `(id, handler)` twice is a no-op. This is required — wrapping in a lambda without this map breaks unsubscribe.
3. Validate the entity is in Localization mode on `Subscribe`; warn + bail otherwise.
4. On `ReleaseAllLocalizationPreloads` / `SoundManager.OnDestroy`, detach all handlers and clear the dictionary. `LocalizedAsset<T>` releases its Addressables handle when the last subscriber detaches.
5. Document in XML on `BroAudio.SubscribeAssetChanged` that the handler lifetime is owned by the caller — same contract as `LocalizedAsset<T>.AssetChanged`.

**Cross-feature:** when preload is active, `LocalizedAsset<AudioClip>` sees the warm cache and resolves synchronously. No extra wiring needed.

**Out of scope:** auto-unsubscribe on scene unload; `IDisposable` wrapper types.

**Done when:**

- Subscribe before locale switch → handler fires once with the current clip.
- Switch `LocalizationSettings.SelectedLocale` → handler fires with the new clip.
- Unsubscribe → no further invocations (verify the lambda-map dispatch).
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
| `LocalizedAsset<T>.AssetChanged` lambda-wrapping breaks unsubscribe | Phase 2 commits to the `(id, handler) → ChangeHandler` map. Do not skip it. |
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
