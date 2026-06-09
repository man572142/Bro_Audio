---
description: A generated proxy code for AudioSource component
---

# IAudioSourceProxy

| NameSpace     | Accessibility |
| ------------- | ------------- |
| Ami.Extension | public        |

## Description

This proxy represents the AudioSource component on the AudioPlayer. It allows you to access all the Unity APIs and properties freely. There’s nothing here but bindings to the AudioSource.

[see the full list of the supported APIs](../../unity-api-integration.md)

### Proxy? Why can't just give us the AudioSource directly?

Since BroAudio utilized [object pooling](../../../core-features/audio-player/#object-pool), we need to reset the AudioSource to its default in order to recycle and reuse safely. There are 3 possible ways to do that:

1\. [MonoBehaviour.Reset()](https://docs.unity3d.com/ScriptReference/MonoBehaviour.Reset.html)? :x: AudioSource doesn’t inherit from MonoBehaviour\
2\. Destroy the component and add it again? :x: That defeats the purpose of object pooling\
3\. Manually reset all properties :white\_check\_mark: seems like the way to go

However, resetting all properties, even those that aren't modified, is inefficient. Unity is natively written in C++, and all C# APIs and properties eventually call the native side, which isn’t free — it produces GC. With around 30 properties in AudioSource, resetting them all after every play session could become an issue, especially since, in most cases, you might have dozens of sounds playing every second.

The proxy class is created after the [IAudioPlayer.AudioSource](iaudioplayer.md#properties) is accessed, and it will record which property you've modified, and only reset them when the player is about to recycle.
