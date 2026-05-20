# `LoadAssetAsync` SoundID-Callback Overloads — Implementation Plan

## 1. Goal

Add `Action<SoundID>`-callback overloads to `BroAudio.LoadAssetAsync` and `BroAudio.LoadAllAssetsAsync` (and their `SoundID` extension-method mirrors) so the caller can preload a clip and chain a play (or any `SoundID`-keyed action) **without writing a closure that captures the `SoundID`**.

The shape matches three existing precedents in BroAudio that already speak the `SoundID` vocabulary:

| Existing | Shape |
|----------|-------|
| `IAudioPlayer.OnEnd(Action<SoundID>)` (`AudioPlayer.Playback.cs:505`) | one-shot, fires with the `SoundID` when playback ends |
| `BroAudio.SubscribeLocalizedClipChanged(SoundID, Action<SoundID>)` (`BroAudio.cs:363`) | ongoing, fires with the `SoundID` on every locale-driven change |
| `SoundID.LocalizedAudioChanged` event (`SoundID.cs:95-99`) | same as above, surfaced as an `event` |

Today, the only way to wire "load then play" is:

```csharp
var handle = BroAudio.LoadAssetAsync(_mySoundID);
handle.Completed += op =>
{
    if (op.Status == AsyncOperationStatus.Succeeded)
        BroAudio.Play(_mySoundID);     // captures _mySoundID → closure allocation
};
```

After this change:

```csharp
BroAudio.LoadAssetAsync(_mySoundID, static id => id.Play());
//                                   ^^^^^^^^^^^^^^^^^^^^^^^
//                                   closure-free static lambda; the SoundID
//                                   travels through the API, not the closure.
```

The return type is **unchanged** (`AsyncOperationHandle<...>`) so the new overloads remain composable with the existing handle-based code paths.

---

## 2. Phases at a glance

| # | Phase | Touches | Depends on |
|---|-------|---------|------------|
| 1 | Internal callback-attach helper | `SoundManager.Addressables.cs` | — |
| 2 | Public overloads in `BroAudio.cs` | `BroAudio.cs` | 1 |
| 3 | Mirror overloads on `SoundID` extensions | `SoundID.cs` | 2 |
| 4 | Tighten `SoundID` extension guard | `SoundID.cs` | — (independent cleanup; bundle for review economy) |

Recommended execution order: **1 → 2 → 3**, with **4** folded into 3 in the same diff. Phases are sequential because each builds on the previous layer's signature.

**Out of scope:** failure callback (`onFailed`), per-clip granular callbacks for the all-assets path, `IDisposable` token / cancellation, async/await wrappers, deprecation of the no-callback overloads. The no-callback overloads stay as the primitive.

---

## 3. Current state (anchors)

| File | Lines | Role |
|------|-------|------|
| `Runtime/BroAudio.cs:306-356` | `#if PACKAGE_ADDRESSABLES \|\| PACKAGE_LOCALIZATION` block | Where the new overloads land |
| `Runtime/BroAudio.cs:324-332` | `LoadAssetAsync(SoundID)` / `LoadAssetAsync(SoundID,int)` | Existing handle-returning entry points |
| `Runtime/BroAudio.cs:318-319` | `LoadAllAssetsAsync(SoundID)` | Existing all-clips entry point |
| `Runtime/SoundManager/SoundManager.Addressables.cs:53-87` | `LoadAllAssetsAsync` / `LoadAssetAsync` | Localization branch already handled here; the new helper attaches the callback to whatever handle these return |
| `Runtime/SoundManager/SoundManager.Localization.cs:29-72` | `LoadLocalizedAssetAsync` / `LoadAllLocalizedAssetsAsync` | Caches handles per `SoundID`; reused as-is |
| `Runtime/DataStruct/SoundID.cs:182-215` | `#if PACKAGE_ADDRESSABLES` extension methods | Where the mirrored extension overloads land |
| `Runtime/DataStruct/SoundID.cs:8-10` | `#if PACKAGE_ADDRESSABLES` import guard | **Inconsistency:** `BroAudio.cs` uses `PACKAGE_ADDRESSABLES \|\| PACKAGE_LOCALIZATION` but the extension methods only compile under `PACKAGE_ADDRESSABLES`. Phase 4 fixes this. |

---

## 4. Design

### 4.1 Public surface

All new members go inside the existing `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION` block in `BroAudio.cs`, and inside the (newly-widened — see Phase 4) matching block in `SoundID.cs`.

```csharp
// BroAudio.cs — new
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, Action<SoundID> onLoaded);
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex, Action<SoundID> onLoaded);
public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id, Action<SoundID> onLoaded);

// SoundID.cs — new (extension mirrors)
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id, Action<SoundID> onLoaded);
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id, int clipIndex, Action<SoundID> onLoaded);
public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(this SoundID id, Action<SoundID> onLoaded);
```

Existing handle-only overloads stay verbatim. The new ones are **strictly additive** — no behavior change for current callers.

### 4.2 Internal helper

Add a single private generic helper to `SoundManager.Addressables.cs` so both `LoadAssetAsync` and `LoadAllAssetsAsync` go through one attachment site. This keeps the failure-skip logic in one place and makes the implementation trivially auditable.

```csharp
// SoundManager.Addressables.cs — new private helper
private static AsyncOperationHandle<T> AttachLoadedCallback<T>(
    AsyncOperationHandle<T> handle, SoundID id, Action<SoundID> onLoaded)
{
    if (onLoaded == null || !handle.IsValid())
    {
        return handle;
    }

    handle.Completed += op =>
    {
        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            onLoaded(id);
        }
    };
    return handle;
}
```

Then expose the SoundManager-level overloads that the static `BroAudio` facade calls:

```csharp
// SoundManager.Addressables.cs — new
public AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex, Action<SoundID> onLoaded)
    => AttachLoadedCallback(LoadAssetAsync(id, clipIndex), id, onLoaded);

public AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id, Action<SoundID> onLoaded)
    => AttachLoadedCallback(LoadAllAssetsAsync(id), id, onLoaded);
```

Notes on the helper:

- `handle.IsValid()` filter means the **invalid-handle** paths (entity not found, not addressable, empty localization references, out-of-range `clipIndex`) silently don't fire the callback. That's the expected contract — same paths that return `default` today.
- `op.Status == AsyncOperationStatus.Succeeded` filter means a **failed** load doesn't fire the callback either. Users who need failure handling continue to chain `handle.Completed` directly. This is consistent with the "Action<SoundID> means the SoundID is now playable" reading.
- The `Completed += handler` subscription works whether the handle is already done or still pending. Per Unity's Addressables docs, *"If the callback is assigned on a completed operation, the callback is deferred until the LateUpdate of the current frame"* (see References). So the callback always fires asynchronously, even for cache-hit Localization handles. Document this as a guaranteed property — callers can rely on "not invoked re-entrantly inside `LoadAssetAsync`".
- No closure allocation chain: the lambda captures `id` and `onLoaded`, both of which the caller passed in. The internal closure is **per-call**, unavoidable for the deferred-dispatch shape, and lighter than the user's prior `op => Manager.Play(_field)` lambda that captures `this`.

### 4.3 BroAudio.cs facade

```csharp
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, Action<SoundID> onLoaded)
    => LoadAssetAsync(id, 0, onLoaded);

public static AsyncOperationHandle<AudioClip> LoadAssetAsync(SoundID id, int clipIndex, Action<SoundID> onLoaded)
    => SoundManager.Instance.LoadAssetAsync(id, clipIndex, onLoaded);

public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(SoundID id, Action<SoundID> onLoaded)
    => SoundManager.Instance.LoadAllAssetsAsync(id, onLoaded);
```

These match the existing pattern of `BroAudio.cs` overloads: zero-index helper delegates to the `(id, clipIndex)` form, both delegate to `SoundManager.Instance`. The `LoadAllAssetsAsync` form has no `clipIndex` parameter (matches existing).

### 4.4 SoundID.cs extension mirrors

```csharp
///<inheritdoc cref="BroAudio.LoadAssetAsync(SoundID, Action{SoundID})"/>
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id, Action<SoundID> onLoaded)
    => BroAudio.LoadAssetAsync(id, onLoaded);

///<inheritdoc cref="BroAudio.LoadAssetAsync(SoundID, int, Action{SoundID})"/>
public static AsyncOperationHandle<AudioClip> LoadAssetAsync(this SoundID id, int clipIndex, Action<SoundID> onLoaded)
    => BroAudio.LoadAssetAsync(id, clipIndex, onLoaded);

///<inheritdoc cref="BroAudio.LoadAllAssetsAsync(SoundID, Action{SoundID})"/>
public static AsyncOperationHandle<IList<AudioClip>> LoadAllAssetsAsync(this SoundID id, Action<SoundID> onLoaded)
    => BroAudio.LoadAllAssetsAsync(id, onLoaded);
```

(Extension forms forward through `BroAudio` rather than `SoundManager.Instance` so the public-API surface remains the single source of truth — same pattern the existing `SoundID.cs:188` extension uses.)

### 4.5 XML doc copy

Each new overload's XML doc must say, in this order:

1. What it does — *"Loads the audio clip ... and invokes `onLoaded` with the `SoundID` once the load completes successfully."*
2. The Localization-mode caveat (for the two `LoadAssetAsync` overloads) — *"In Localization mode, `clipIndex` is ignored and the active locale's clip is loaded."* (mirrors existing doc on `BroAudio.cs:329-330`)
3. The dispatch guarantee — *"The callback is invoked on the main thread on a later frame, never re-entrantly inside `LoadAssetAsync`. If the load fails or the entity is invalid, the callback is not invoked."*
4. The composability note — *"The returned `AsyncOperationHandle` can be awaited or have additional `Completed` handlers attached for failure handling."*

---

## 5. Phase 4 — `SoundID` extension guard widening

`SoundID.cs:8-10` and `:182-215` guard the load/release/key extension methods on `#if PACKAGE_ADDRESSABLES` only, while the corresponding `BroAudio.cs:306-356` static methods live under `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION`. In a Localization-only project (Localization installed, Addressables direct dependency omitted — Localization pulls it in transitively, so this is rare but legal), the static methods compile but the extension forms don't. Bundle this fix with the new overloads:

- Change `#if PACKAGE_ADDRESSABLES` to `#if PACKAGE_ADDRESSABLES || PACKAGE_LOCALIZATION` at `SoundID.cs:8` and `SoundID.cs:182`.
- No other changes needed — all referenced types (`AsyncOperationHandle`, `AudioClip`, `IList`) are already available when either package is present.

---

## 6. Edge cases & semantics

| Case | Behavior |
|------|----------|
| `onLoaded == null` | Returns the handle as-is, equivalent to the no-callback overload. No `Completed` subscription added. |
| Entity not found / not addressable / localization refs empty | `LoadAssetAsync` already returns `default` (invalid handle). `AttachLoadedCallback` short-circuits on `!handle.IsValid()` → no callback. No exception, no log beyond what the existing path already emits (`SoundManager.Addressables.cs:127` / `Localization.cs:248`). |
| `clipIndex` out of range (Addressables mode) | Same as above — `LoadAssetAsync(id, clipIndex)` returns `default`, callback never fires. |
| Already-loaded Addressables clip | `BroAudioClip.LoadAssetAsync` (line 19-27) returns the existing handle. Unity defers the `Completed` invocation to the current frame's LateUpdate, so the callback fires within one frame. |
| Already-loaded Localization clip | `LoadLocalizedAssetAsync` (line 38-41) returns the cached handle. Same deferred LateUpdate dispatch. |
| Same `(id, onLoaded)` passed twice | Two independent `Completed` subscriptions on the same handle → callback fires twice. Match Unity's underlying semantics; do not deduplicate. (Distinct from `IAudioPlayer.OnEnd`, which dedupes — but `OnEnd` is a persistent subscription, this is a one-shot.) |
| Caller calls `ReleaseAsset(id)` between `LoadAssetAsync` and completion | Addressables invalidates the handle. The `op.Status == Succeeded` check still guards the user's callback because a released handle's completion does not transition to `Succeeded`. |
| Locale switch mid-load (Localization mode) | `LocalizationSettings.SelectedLocaleChanged` handler in `SoundManager.Localization.cs:100-114` releases the cached handle. The in-flight callback may see `Status != Succeeded` and skip — correct. Callers that want locale-change notifications use `SubscribeLocalizedClipChanged` instead; the new `LoadAssetAsync` callback is **one-shot, load-only**. |
| Localization-mode `LoadAllAssetsAsync` | The chained handle (`SoundManager.Localization.cs:67-71`) completes when the inner clip handle completes. The callback fires once after the chain wraps the single clip into the `IList`. Same semantics as the Addressables `LoadAllAssetsAsync` group-completion. |
| Threading | `AsyncOperationHandle.Completed` is invoked via Addressables' `DelayedActionManager` on Unity's player loop — main thread. Calling `BroAudio.Play` from the callback is safe. |

---

## 7. Done when

- Project compiles with `PACKAGE_ADDRESSABLES` defined alone, `PACKAGE_LOCALIZATION` defined alone, both defined, and neither defined.
- Existing callers compile unchanged — all new overloads are strictly additive.
- `BroAudio.LoadAssetAsync(id, id2 => Played(id2))` invokes the handler **exactly once** with `id == id2`, after Addressables completes the load.
- The same call after the clip is already loaded fires the handler within one frame (LateUpdate of the call frame), never re-entrantly.
- Invalid `SoundID` (entity missing / wrong mode / out-of-range index) → callback never fires.
- Failed load (e.g., Addressables key missing in build) → callback never fires; the returned handle's `Status` is `Failed`.
- Localization-mode entity with valid table+entry → callback fires with the original `SoundID`, regardless of which locale resolved it.
- `SoundID.LoadAssetAsync(this SoundID, Action<SoundID>)` extension form compiles in a project that has Localization but not Addressables directly (Phase 4 verification).
- XML docs render correctly in IDE tooltips and the four-point structure from §4.5 is preserved.

---

## 8. Risks

| Risk | Mitigation |
|------|------------|
| Caller assumes the callback fires synchronously when the clip is already loaded, writes code relying on that ordering | XML doc §4.5 item 3 explicitly states "later frame, never re-entrantly". The Phase 1 internal helper uniformly defers via `Completed += …`; no fast path that fires inline. |
| Caller expects a failure callback and gets silence | XML doc §4.5 item 3 + the docs item 4 ("…can have additional `Completed` handlers attached for failure handling") direct power users to the existing handle pattern. Out of scope for v1. |
| Same `(id, handler)` registered twice → double-fire surprises caller | Documented in §6. Matches Unity's underlying `Completed` semantics. If user pain emerges, revisit with an opt-in dedup overload — do not retrofit silently. |
| Closure created inside `AttachLoadedCallback` defeats the user-facing "no closure" framing | The internal closure captures only the two arguments the user passed; the *user's* call site is closure-free for fields/locals. That's the win — not zero allocations, but zero captured-state lambdas at the call site. State this explicitly in the §1 motivation. |
| Extension-method guard widening (Phase 4) silently changes API visibility for projects that have Localization but somehow lack Addressables | Localization 1.4+ depends on Addressables, so this surface always lights up together in practice. Even in pathological dependency arrangements, all the new code-path symbols (`AsyncOperationHandle`, `AudioClip`) are independently available. No runtime risk; compile-time surface only grows. |

---

## 9. References

- Addressables `AsyncOperationHandle<T>.Completed` (deferred-when-already-done behavior): <https://docs.unity3d.com/Packages/com.unity.addressables@2.0/manual/AddressableAssetsAsyncOperationHandle.html>
- Unity Localization `LocalizedAsset<T>` (used by `SoundManager.Localization.cs`): <https://docs.unity3d.com/Packages/com.unity.localization@1.4/api/UnityEngine.Localization.LocalizedAsset-1.html>
- Existing `Action<SoundID>` precedents in this codebase:
  - `IAudioPlayer.OnEnd` — `Assets/BroAudio/Runtime/Player/AudioPlayer.Playback.cs:505`
  - `BroAudio.SubscribeLocalizedClipChanged` — `Assets/BroAudio/Runtime/BroAudio.cs:363`
  - `SoundID.LocalizedAudioChanged` — `Assets/BroAudio/Runtime/DataStruct/SoundID.cs:95`
- Companion plan: `Docs/localization-workflow-improvement-plan.md` (Phase 1 unified `LoadAssetAsync` across modes; this plan layers the callback overloads on top).
