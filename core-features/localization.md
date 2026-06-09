---
cover: >-
  https://images.unsplash.com/photo-1555564883-acd71fa27745?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw0fHx0cmFuc2xhdGlvbnxlbnwwfHx8fDE3ODEwMTQwMjF8MA&ixlib=rb-4.1.0&q=85
coverY: -240.83600614859645
layout:
  width: default
  cover:
    visible: true
    size: full
  title:
    visible: true
  description:
    visible: false
  tableOfContents:
    visible: true
  outline:
    visible: true
  pagination:
    visible: true
  metadata:
    visible: true
  tags:
    visible: true
  actions:
    visible: true
---

# Localization

## Introduction

[Localization](https://docs.unity3d.com/Packages/com.unity.localization/manual/index.html) is Unity's official solution for adapting your game's content to different languages and regions. BroAudio integrates with it so a single [SoundID](../reference/api-documentation/struct/audioid.md) can resolve to a different audio clip depending on the player's currently active locale — perfect for localized voice-over, narration, or any sound that needs to change with the language.

{% hint style="warning" %}
This documentation covers using Localization **in BroAudio**. For further details and the official Localization manual, please refer to the [Unity Manual](https://docs.unity3d.com/Packages/com.unity.localization/manual/index.html).
{% endhint %}

{% hint style="info" %}
Unity's Localization is built on top of [Addressables](addressables.md). Switching an entity to Localization mode automatically force-enables Addressables for that entity, and localized clips are loaded and released through the same async API. Read the [Addressables](addressables.md) page first if you're not familiar with it.
{% endhint %}

## How To Use?

To install the Localization package, please follow the instructions in [Unity's manual](https://docs.unity3d.com/Packages/com.unity.localization/manual/Installation.html), then set up your project's locales and creates at least one [Asset Table](https://docs.unity3d.com/Packages/com.unity.localization/manual/AssetTables.html). Once the package is installed, BroAudio will automatically unlock all localization related options. No further configuration on the BroAudio side is needed!

### Switching an Entity to Localization Mode

Open the [Library Manager](library-manager/), select an entity, and set its **Play Mode** to <mark style="color:orange;">**Localization**</mark>.

Because Localization mode resolves clips through Asset Tables rather than the entity's own clip list, switching to it changes how the entity stores clips. **If the entity already contains clips, a confirmation window will appear:**

> Switching to Localization mode will clear all AudioClip references and clip properties on this entity, and force-enable Addressables for this entity. Continue?

* **\[Yes]** — Clears the existing AudioClip references and per-clip settings, enables Addressables on the entity, and switches to Localization mode.
* **\[No]** — Keeps the previous Play Mode; no changes are made.

### Assigning Clips per Locale

Once in Localization mode, the Clips tab shows a dedicated layout:

* The **first row** contains two dropdowns:
  * **Asset Table** — the Localization Asset Table Collection to pull clips from.
  * **Table Entry** — the key (entry) within that table this entity maps to.
  * A button on the left opens the **Localization Tables** window (`Window/Asset Management/Localization Tables`) for managing tables directly.
* Below that, BroAudio lists **one row per locale** defined in your project. For each locale you can:
  * Assign an `AudioClip` via the object picker or by dragging and dropping a clip onto the row.
  * Adjust a per-locale **Volume**.
  * **Preview** the clip with the play button.

Assigning a clip here writes it straight into the selected Asset Table for that locale, so the table and your BroAudio entity always stay in sync. The locale rows update automatically whenever you add or remove locales in your project's Localization settings.

## Playing Localized Audio

Playing a Localized entity is no different from playing any other sound, just call `Play` with its [SoundID](../reference/api-documentation/struct/audioid.md):

```csharp
BroAudio.Play(SoundID id);
```

BroAudio resolves the clip whose locale matches `LocalizationSettings.SelectedLocale` at the moment of playback. When the player changes the active locale, the next `Play` call will automatically use the clip for the new locale.

## Loading the asset

Because Localization mode is backed by Addressables, the clip for the active locale must be loaded before playback. The loading API is identical to the [Addressables](addressables.md#loading-the-asset) one — call the loading method, wait for the [AsyncOperationHandle](https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/AddressableAssetsAsyncOperationHandle.html) to complete, then release it when it's no longer needed.

{% hint style="warning" %}
If a localized clip has not been preloaded, BroAudio will resolve it **synchronously** on `Play`, which blocks the main thread until the clip finishes loading. Call `LoadAssetAsync(SoundID)` ahead of time to avoid playback hitches.
{% endhint %}

In Localization mode, the loading methods always operate on the active locale's clip, so any `clipIndex` parameter is ignored.

### Public Methods in [BroAudio](../reference/api-documentation/class/broaudio.md) class

<table data-full-width="false"><thead><tr><th width="159">Method</th><th width="205">Return</th><th width="133">Parameters</th><th width="251">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>LoadAssetAsync</strong></mark></td><td><a href="https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/AddressableAssetsAsyncOperationHandle.html">AsyncOperationHandle</a>&#x3C;AudioClip></td><td><a href="../reference/api-documentation/struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td>Preloads the clip for the currently active locale</td></tr><tr><td><mark style="color:orange;"><strong>LoadAllAssetsAsync</strong></mark></td><td><a href="https://docs.unity3d.com/Packages/com.unity.addressables@1.22/manual/AddressableAssetsAsyncOperationHandle.html">AsyncOperationHandle</a>&#x3C;IList&#x3C;AudioClip>></td><td><a href="../reference/api-documentation/struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td>Preloads the active locale's clip, returned as a single-item list</td></tr><tr><td><mark style="color:orange;"><strong>ReleaseAsset</strong></mark></td><td>void</td><td><a href="../reference/api-documentation/struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td>Releases the active locale's clip (any <mark style="color:green;">int</mark> <code>clipIndex</code> is ignored)</td></tr><tr><td><mark style="color:orange;"><strong>ReleaseAllAssets</strong></mark></td><td>void</td><td><a href="../reference/api-documentation/struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td>Releases the loaded localized clip for the entity</td></tr></tbody></table>

## Reacting to Localized Audio Changes

When the localized clip changes, you often want to react — for example, restarting a piece of localized voice-over in the new language. BroAudio exposes this notification through the [SoundID](../reference/api-documentation/struct/audioid.md) so you can subscribe per sound.

`LocalizedAudioChanged` wraps Unity Localization's `LocalizedAsset<T>.AssetChanged`, so it inherits the same side effect: the moment you subscribe, Unity starts loading the clip for the **currently active locale** and fires your handler once that load completes — not only on subsequent locale changes. In other words, subscribing both:

* **Preloads** the active locale's clip (holding an Addressables reference for it), and
* **Invokes your handler once right away**, in addition to every later locale change.

So a handler that calls `BroAudio.Play(id)` on subscribe (e.g. in `OnEnable`) will play the sound immediately, not just when the language is switched afterward. If that isn't what you want, guard the first invocation or subscribe only at the point you actually intend playback to begin.
{% endhint %}

{% tabs %}
{% tab title="Event" %}
Subscribe to the `LocalizedAudioChanged` event on the [SoundID](../reference/api-documentation/struct/audioid.md):

```csharp
void OnEnable()
{
    _voiceID.LocalizedAudioChanged += OnVoiceLocaleChanged;
}

void OnDisable()
{
    _voiceID.LocalizedAudioChanged -= OnVoiceLocaleChanged;
}

void OnVoiceLocaleChanged(SoundID id)
{
    BroAudio.Play(id);
}
```
{% endtab %}

{% tab title="Auto-replay adapter" %}
If all you want is to replay the sound whenever its locale changes, use the built-in `PlayOnLocalizedAudioChanged` adapter:

```csharp
_voiceID.LocalizedAudioChanged += BroAudio.PlayOnLocalizedAudioChanged;
```
{% endtab %}

{% tab title="Static methods" %}
You can also subscribe directly through the [BroAudio](../reference/api-documentation/class/broaudio.md) class:

```csharp
BroAudio.SubscribeLocalizedAudioChanged(id, OnVoiceLocaleChanged);
BroAudio.UnsubscribeLocalizedAudioChanged(id, OnVoiceLocaleChanged);
```
{% endtab %}
{% endtabs %}

{% hint style="info" %}
Because subscribing loads the asset, always unsubscribe handlers you no longer need. Unsubscribing the last handler for a [SoundID](../reference/api-documentation/struct/audioid.md) — and with no preload outstanding — lets BroAudio release the underlying Addressables handle automatically, so balance every subscribe with an unsubscribe (typically `OnEnable`/`OnDisable`) to avoid leaking the loaded clip.
{% endhint %}
