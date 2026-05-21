# Localization Unification — Route Through `LocalizedAudioClip` on the Entity

## Background

`SoundManager.Localization.cs` currently maintains three parallel mechanisms for resolving Localization-mode audio clips:

| Concern | Mechanism | Per-id state held |
|---|---|---|
| Preload / Release / `IsLoaded` | `LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync(table, entry)` | `_preloadedLocalizationClips`, `_inflightLocalizationOps` |
| `AssetChanged` subscription | `new LocalizedAsset<AudioClip>{ TableReference, TableEntryReference }; .AssetChanged += handler` | `_localizedAssets`, `_localizedClipHandlers` |
| Playback resolve | `GetLocalizedAssetAsync(table, entry).WaitForCompletion()` inside `LocalizationClipStrategy.SelectClip` | none |

This has two structural problems:

1. **Three sources of truth.** `IsLoaded`, `AssetChanged`, and `SelectClip` don't share state. Preloading a `SoundID` does not let `SelectClip` skip its synchronous `GetLocalizedAssetAsync` call on the next play, so the "was not preloaded" warning fires even after a successful preload.
2. **Cached `AsyncOperationHandle<AudioClip>` is unreliable.** Unity Localization pools internal `LoadAssetOperation` instances and recycles them after `Completed`. The saved handle struct reports `IsValid() == false` shortly after, breaking `IsLoaded` and the original `Addressables.Release` path. The current code in `SoundManager.Localization.cs` already works around this by caching the `AudioClip` plus `(TableReference, TableEntryReference)` instead of the handle (per `localization-clip-cache-plan.md`).

The chosen fix is to route all three paths through a single `LocalizedAudioClip` field on the entity itself, so the entity owns the (table, entry) pair and the SoundManager-side runtime cache only tracks per-session lifecycle state.

## Goals

1. `BroAudio.IsLoaded(id)` returns `true` for any Localization-mode entity whose clip was preloaded and not yet released, for the active locale.
2. `BroAudio.ReleaseAsset(id)` / `ReleaseAllAssets(id)` reliably release the underlying Localization asset for the current locale via Localization's own release API.
3. `OnSelectedLocaleChanged` invalidates `CurrentClip` so the playback cache-hit path doesn't serve the previous locale's clip during the gap before `AssetChanged` fires with the new locale's clip.
4. `LocalizationClipStrategy.SelectClip` consults the SoundManager cache and skips the synchronous `GetLocalizedAssetAsync` call on cache-hit — eliminating the "was not preloaded" warning after a successful preload.
5. Public API of `BroAudio.LoadAssetAsync(SoundID)` keeps its `AsyncOperationHandle<AudioClip>` return type — `NewMonoBehaviourScript.PreloadAndPlay()` and other handle-based callers keep working.
6. `Subscribe`/`Unsubscribe` semantics stay identical from the caller's perspective.
7. No change to the Addressables-mode path.

## Non-goals

- Adopting Localization's **Preload Tables** workflow. Separate, larger workflow change deferred.
- Backwards-compatibility for entities serialized with the old `_localizationTable` / `_localizationEntry` fields. This is a hard break on the `DEV_Unity6` branch; existing assets must re-pick their table/entry.

## Design

### Entity data model

Replace the two separate refs with a single Unity-idiomatic `LocalizedAudioClip` field (`LocalizedAudioClip` is the sealed `LocalizedAsset<AudioClip>` Unity ships with `Unity.Localization`):

```csharp
// AudioEntity.Localization.cs
public partial class AudioEntity
{
    [SerializeField] private LocalizedAudioClip _localizedAudio;
    public LocalizedAudioClip LocalizedAudio => _localizedAudio;

#if UNITY_EDITOR
    public static class LocalizationEditorPropertyName
    {
        public const string LocalizedAudio = "_localizedAudio";
        public const string Table         = "_localizedAudio.m_TableReference";
        public const string Entry         = "_localizedAudio.m_TableEntryReference";
    }
#endif
}
```

The old `_localizationTable` and `_localizationEntry` fields and their constants are deleted outright. No `[FormerlySerializedAs]`, no fallback.

### Editor inspector

`ReorderableClips.Localization.cs` retargets its `SerializedProperty` lookups against the nested paths:

- `_localizationTableProp = serializedObject.FindProperty("_localizedAudio.m_TableReference");`
- `_localizationEntryProp = serializedObject.FindProperty("_localizedAudio.m_TableEntryReference");`
- `FindPropertyRelative(TableCollectionNameField)` / `FindPropertyRelative(EntryKeyField)` / `FindPropertyRelative(EntryKeyIdField)` calls work unchanged — the property layout under `m_TableReference` and `m_TableEntryReference` is what `TableReference` and `TableEntryReference` always serialized.

The custom split-dropdown UI (Asset Table picker + Table Entry picker, with locale-clip rows underneath) stays as today. We do not adopt Unity's combined `LocalizedAudioClip` drawer.

### Runtime cache

One dictionary in `SoundManager.Localization.cs`:

```csharp
private Dictionary<SoundID, LocalizedRuntimeEntry> _localizedRuntime;

private sealed class LocalizedRuntimeEntry
{
    public AudioEntity Entity;                               // cached so teardown doesn't re-resolve
    public AudioClip CurrentClip;                            // updated by Tracker on AssetChanged
    public AsyncOperationHandle<AudioClip> PreloadHandle;    // valid only while IsPreloaded
    public bool IsPreloaded;
    public LocalizedAsset<AudioClip>.ChangeHandler Tracker;  // one per entry lifetime
    public Dictionary<Action<SoundID>, LocalizedAsset<AudioClip>.ChangeHandler> UserHandlers;
}
```

**Invariant:** an entry exists IFF `IsPreloaded || UserHandlers.Count > 0`. The Tracker subscription is what keeps the Localization asset loaded when only Subscribers are holding it; the `PreloadHandle` is what holds it loaded when Preload is in effect.

### Fields removed

- `_preloadedLocalizationClips : Dictionary<SoundID, LocalizedClipCacheEntry>`
- `_inflightLocalizationOps : Dictionary<SoundID, AsyncOperationHandle<AudioClip>>`
- `_localizedAssets : Dictionary<SoundID, LocalizedAsset<AudioClip>>`
- `_localizedClipHandlers : Dictionary<(SoundID, Action<SoundID>), LocalizedAsset<AudioClip>.ChangeHandler>`
- `LocalizedClipCacheEntry` struct.

### Fields kept

- `_isSubscribedToLocaleChanged : bool` — still gates the one-time `LocalizationSettings.SelectedLocaleChanged` subscription, now triggered by `EnsureLocaleChangedSubscribed()` on first entry creation rather than on first preload.

### Lifecycle helpers

```csharp
private LocalizedRuntimeEntry GetOrCreateEntry(SoundID id, AudioEntity entity)
{
    _localizedRuntime ??= new Dictionary<SoundID, LocalizedRuntimeEntry>();
    if (_localizedRuntime.TryGetValue(id, out var entry))
    {
        return entry;
    }

    EnsureLocaleChangedSubscribed();

    entry = new LocalizedRuntimeEntry
    {
        Entity = entity,
        UserHandlers = new Dictionary<Action<SoundID>, LocalizedAsset<AudioClip>.ChangeHandler>(),
    };
    entry.Tracker = clip => entry.CurrentClip = clip;
    entity.LocalizedAudio.AssetChanged += entry.Tracker;   // fires immediately with current locale's asset
    _localizedRuntime[id] = entry;
    return entry;
}

private void MaybeTearDownEntry(SoundID id, LocalizedRuntimeEntry entry)
{
    if (entry.IsPreloaded || entry.UserHandlers.Count > 0)
    {
        return;
    }

    entry.Entity.LocalizedAudio.AssetChanged -= entry.Tracker;
    _localizedRuntime.Remove(id);
}
```

### Preload — `LoadLocalizedAssetAsync(SoundID id, AudioEntity entity)`

```csharp
if (!HasValidLocalizationReferences(entity.LocalizedAudio))
{
    return default;
}

var entry = GetOrCreateEntry(id, entity);

if (entry.IsPreloaded)
{
    return entry.CurrentClip != null
        ? Addressables.ResourceManager.CreateCompletedOperation(entry.CurrentClip, string.Empty)
        : entry.PreloadHandle;   // still inflight from a prior call — reuse
}

entry.PreloadHandle = entity.LocalizedAudio.LoadAssetAsync();
entry.IsPreloaded = true;
return entry.PreloadHandle;
```

`HasValidLocalizationReferences` is updated to accept `LocalizedAudioClip` and inspect `clip.TableReference.ReferenceType` / `clip.TableEntryReference.ReferenceType`.

`LoadAllLocalizedAssetsAsync` is unchanged in shape — it still calls `LoadLocalizedAssetAsync` and wraps the result in `CreateChainOperation`. The cache-hit path returns a `CreateCompletedOperation`, which the chain accepts.

### `IsLoaded` — `IsLocalizationClipLoaded(SoundID id)`

```csharp
return _localizedRuntime != null
    && _localizedRuntime.TryGetValue(id, out var entry)
    && entry.IsPreloaded
    && entry.CurrentClip != null;
```

`IsPreloaded` is required so that "subscribed but never preloaded" doesn't report `true`, matching the existing semantics.

### Release — `ReleaseLocalizationClipInternal(SoundID id)`

```csharp
if (_localizedRuntime == null || !_localizedRuntime.TryGetValue(id, out var entry) || !entry.IsPreloaded)
{
    return;
}

LocalizationSettings.AssetDatabase.ReleaseAsset(
    entry.Entity.LocalizedAudio.TableReference,
    entry.Entity.LocalizedAudio.TableEntryReference);

entry.IsPreloaded = false;
entry.PreloadHandle = default;
MaybeTearDownEntry(id, entry);
```

Single Release wipes the preload ref. If user handlers exist, the Tracker stays subscribed and Unity keeps the asset alive for them — `IsLoaded(id)` returns `false` (preload is released) but `Subscribe` callers continue to receive locale-change events. This matches the rule chosen during brainstorming: Preload and Subscribe are independent lifetime contributors.

### Subscribe — `SubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)`

```csharp
if (handler == null) return;

if (!TryGetLocalizationEntity(id, out var entity))
{
    Debug.LogWarning(Utility.LogTitle + $"SubscribeLocalizedAudioChanged: entity for SoundID '{id}' is not in Localization mode.");
    return;
}

if (!HasValidLocalizationReferences(entity.LocalizedAudio))
{
    return;
}

var entry = GetOrCreateEntry(id, entity);
if (entry.UserHandlers.ContainsKey(handler))
{
    return;
}

LocalizedAsset<AudioClip>.ChangeHandler wrapper = _ => handler(id);
entity.LocalizedAudio.AssetChanged += wrapper;
entry.UserHandlers[handler] = wrapper;
```

Unity dispatches `AssetChanged` immediately to a new subscriber with the current asset, so the handler fires once on subscribe with the active locale's clip (matches Unity's documented behavior; matches today's BroAudio behavior).

### Unsubscribe — `UnsubscribeLocalizedAudioChanged(SoundID id, Action<SoundID> handler)`

```csharp
if (handler == null || _localizedRuntime == null) return;

if (!_localizedRuntime.TryGetValue(id, out var entry)) return;
if (!entry.UserHandlers.TryGetValue(handler, out var wrapper)) return;

entry.Entity.LocalizedAudio.AssetChanged -= wrapper;
entry.UserHandlers.Remove(handler);
MaybeTearDownEntry(id, entry);
```

### Locale change — `OnSelectedLocaleChanged(Locale newLocale)`

```csharp
if (_localizedRuntime == null) return;

foreach (var entry in _localizedRuntime.Values)
{
    entry.CurrentClip = null;
}
// Unity will reload each entry's asset for the new locale and fire AssetChanged on its
// subscribers — the Tracker on each entry repopulates CurrentClip.
```

The handler's sole job is to invalidate `CurrentClip` so `SelectClip`'s cache-hit path doesn't serve the previous locale's clip during the window between locale switch and the new `AssetChanged` firing. No manual `ReleaseAsset` loop — Unity handles release+reload internally because we hold the asset via AssetChanged subscription, not via direct table/entry refs.

`EnsureLocaleChangedSubscribed()` does the one-time `LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged` registration on first entry creation, using the existing `_isSubscribedToLocaleChanged` flag.

### Shutdown — `ReleaseAllLocalizationPreloads()`

```csharp
if (_localizedRuntime != null)
{
    foreach (var entry in _localizedRuntime.Values)
    {
        if (entry.IsPreloaded)
        {
            LocalizationSettings.AssetDatabase.ReleaseAsset(
                entry.Entity.LocalizedAudio.TableReference,
                entry.Entity.LocalizedAudio.TableEntryReference);
        }

        foreach (var kv in entry.UserHandlers)
        {
            entry.Entity.LocalizedAudio.AssetChanged -= kv.Value;
        }

        entry.Entity.LocalizedAudio.AssetChanged -= entry.Tracker;
    }
    _localizedRuntime.Clear();
}

if (_isSubscribedToLocaleChanged)
{
    LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    _isSubscribedToLocaleChanged = false;
}
```

### Playback path — `LocalizationClipStrategy`

New SoundManager internal:

```csharp
internal bool TryGetCachedLocalizedClip(SoundID id, out AudioClip clip)
{
    clip = null;
    if (_localizedRuntime != null
        && _localizedRuntime.TryGetValue(id, out var entry)
        && entry.CurrentClip != null)
    {
        clip = entry.CurrentClip;
        return true;
    }
    return false;
}
```

`LocalizationClipStrategy` is restructured:

- Replace `_table : TableReference` and `_entry : TableEntryReference` with `_localizedAudio : LocalizedAudioClip` and `_id : SoundID`.
- `Inject(SoundID id, LocalizedAudioClip localizedAudio, string entityName)` — the SoundID is required for the cache lookup; callers already have it at injection time.
- `SelectClip`:

```csharp
if (!HasValidReferences(_localizedAudio))
{
    Debug.LogError(...); index = -1; return null;
}

AudioClip resolvedClip;
if (SoundManager.Instance.TryGetCachedLocalizedClip(_id, out var cached))
{
    resolvedClip = cached;
}
else
{
    var handle = _localizedAudio.LoadAssetAsync();
    if (!handle.IsDone)
    {
        Debug.LogWarning(Utility.LogTitle +
            $"Localized AudioClip for entity '{_entityName}' was not preloaded; " +
            $"resolving synchronously will block the main thread. " +
            $"Call {nameof(BroAudio)}.{nameof(BroAudio.LoadAssetAsync)}(SoundID) before playback to avoid hitches.");
    }
    resolvedClip = handle.WaitForCompletion();
}

// remainder identical to today: null check, match clip row by selectedLocale.Identifier,
// wrap in LocalizedBroAudioClipWrapper.
```

The cache-miss `LoadAssetAsync()` bumps Unity's refcount by one and the handle ref leaks (we don't release it after `WaitForCompletion`). This is unchanged from today's behavior — the existing code calls `GetLocalizedAssetAsync` and discards the handle the same way. The cache-hit path avoids the issue entirely, which is the intended fast path; the leak is the documented cost of "playing without preloading."

## Implementation Steps

1. **Entity schema swap.**
   - Replace `_localizationTable` / `_localizationEntry` fields in `AudioEntity.Localization.cs` with `_localizedAudio : LocalizedAudioClip`.
   - Update `LocalizationEditorPropertyName` constants.
   - Delete `LocalizationTable` / `LocalizationEntry` accessors. Add `LocalizedAudio` accessor.

2. **Editor inspector retarget.** Update `ReorderableClips.Localization.cs` `FindProperty` calls and any other consumer of the old property name constants (see `LocalizationEditorPropertyName.LocalizationTable` references) to use the nested paths.

3. **Strategy injection signature change.** Update `LocalizationClipStrategy.Inject` to `(SoundID id, LocalizedAudioClip localizedAudio, string entityName)` and adjust callers (search for `LocalizationClipStrategy.Inject` and locate where Inject is wired in `AudioEntity` clip-strategy construction).

4. **Add `TryGetCachedLocalizedClip` on `SoundManager`** (internal, in `SoundManager.Localization.cs`).

5. **Rewrite `SoundManager.Localization.cs`:**
   - Remove old fields and `LocalizedClipCacheEntry` struct.
   - Add `LocalizedRuntimeEntry` class and `_localizedRuntime` dictionary.
   - Implement `GetOrCreateEntry`, `MaybeTearDownEntry`, `EnsureLocaleChangedSubscribed`.
   - Rewrite `LoadLocalizedAssetAsync`, `IsLocalizationClipLoaded`, `ReleaseLocalizationClipInternal`, `SubscribeLocalizedAudioChanged`, `UnsubscribeLocalizedAudioChanged`, `OnSelectedLocaleChanged`, `ReleaseAllLocalizationPreloads`.
   - Update `HasValidLocalizationReferences` signature to accept `LocalizedAudioClip`.

6. **Update `LocalizationClipStrategy.SelectClip`** with the cache-lookup-first flow.

7. **Verify `LoadAllLocalizedAssetsAsync`** still works when fed a `CreateCompletedOperation` handle from the cache-hit path.

8. **Smoke-test the integration points:** `BroAudio.LoadAssetAsync(SoundID)` callers in `NewMonoBehaviourScript`, the `SoundID.LocalizedAudioChanged` event in `SoundID.cs`, and any tests under `Assets/Tests/` touching Localization mode.

## Edge Cases

- **Preload twice before first completes.** Second call hits `IsPreloaded == true && CurrentClip == null` and returns the in-flight `PreloadHandle`. Both callers' `Completed +=` fire on the same op. No duplicate load.
- **Subscribe after Preload.** `GetOrCreateEntry` finds the existing entry; Tracker already subscribed and `CurrentClip` populated. New subscriber's wrapper receives an immediate fire from Unity with the current asset.
- **Preload after Subscribe.** Entry exists, Tracker subscribed, `CurrentClip` populated. `LoadAssetAsync()` on an already-loaded asset returns a handle that completes next frame with the cached clip.
- **Release while subscribers exist.** `IsPreloaded → false`, Tracker stays subscribed, asset stays loaded for subscribers. `IsLoaded(id) == false`; subscribers still get locale-change events. Intentional.
- **Locale change with active preload.** Unity reloads the asset for the existing AssetChanged subscription; our `OnSelectedLocaleChanged` nulls `CurrentClip` first → `IsLoaded` briefly false → Tracker repopulates. `IsPreloaded` stays true throughout — the preload carries over to the new locale. The original `PreloadHandle` from the old locale becomes stale per Localization pooling, but we never dereference it after `Completed`.
- **Entity removed from library at runtime.** `LocalizedRuntimeEntry.Entity` caches the reference so teardown still has access to `LocalizedAudio` for unsubscription. If the entity reference itself becomes null (e.g., asset deleted), the unsubscribe is a no-op and the entry is removed regardless.
- **`LocalizedAudio` unset on the entity.** `HasValidLocalizationReferences` rejects up-front; no entry created. Warning logged once per call.
- **`AudioClip` becomes null mid-cache** (domain reload, `UnloadUnusedAssets`). `IsLocalizationClipLoaded` treats null as not-loaded. The next `SelectClip` falls back to `LoadAssetAsync().WaitForCompletion()` and emits the "was not preloaded" warning — acceptable degradation.

## Testing

Manual test plan (mirrors `localization-clip-cache-plan.md` plus the new cache-hit verification):

**Preload/Release stale-handle regression:**
1. Localization-mode entity, `PreloadAndPlay()` from `NewMonoBehaviourScript`.
2. Wait 3 seconds.
3. `BroAudio.IsLoaded(soundID)` → `true`.
4. `BroAudio.ReleaseAsset(soundID)`.
5. `BroAudio.IsLoaded(soundID)` → `false`.

**Locale change with active preload:**
1. Preload entity in zh-TW.
2. Switch locale to en.
3. Immediately call `IsLoaded(soundID)` — briefly `false` during reload, `true` again once Tracker fires.
4. `Play(soundID)` — plays the en clip with no "was not preloaded" warning.

**Multiple-preload idempotency:**
1. Call `LoadAssetAsync(id)` twice back-to-back before the first completes.
2. Both returned handles' `Completed` fire with the same `AudioClip`.
3. After both complete, exactly one cache entry exists.
4. A single `ReleaseAsset(id)` clears the preload (cache may persist if subscribers exist).

**Play-after-preload regression — no warning:**
1. `PreloadAndPlay()`, wait 3 seconds.
2. `Play()` again — `SelectClip` cache-hits, no "was not preloaded" warning, no `GetLocalizedAssetAsync` call (verifiable via Localization debug log).

**Subscribe / Unsubscribe lifetime:**
1. Subscribe a handler → handler fires once with the current locale's clip.
2. Switch locale → handler fires with the new locale's clip.
3. Preload then Release → handler still fires on subsequent locale switches (asset alive via subscription).
4. Unsubscribe → no further fires; if no preload outstanding, entry is removed.

**Shutdown clean-up:**
1. Preload several entities, subscribe handlers on some.
2. Trigger SoundManager destroy (scene unload).
3. No exceptions; no `LocalizedAsset` retains delegate references; subsequent SoundManager init starts from a clean slate.

## Out-of-scope follow-ups

- **Preload Tables migration** — separate workflow change.
- **Public `IReadOnlyAudioEntity.LocalizedAudio` exposure** — `LocalizedAudio` is currently public on the implementation; whether to surface it through the read-only interface is a deliberate API decision deferred to a separate change.
