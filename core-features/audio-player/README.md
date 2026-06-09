---
coverY: 0
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

# Audio Player

## Introduction

Audio Player is the base object that is used to play audio. It's a GameObject that contains an [AudioSource](https://docs.unity3d.com/ScriptReference/AudioSource.html) component and spawns in the scene when needed.&#x20;

## How To Use?

Simply just use the [SoundSource](../../overview/getting-started.md#without-code) component or call the [BroAudio.Play(\_soundID)](../../reference/api-documentation/class/broaudio.md#playback) API in your script. Bro will instantiate an AudioPlayer and automatically set all the properties you configured in the [LibraryManager](../library-manager/) to its AudioSource component.

### Control the AudioPlayer

#### Play by <img src="../../.gitbook/assets/BroAudio_Logo_900x900.png" alt="" data-size="line">SoundSource component

SoundSource component has many public methods that could be used with [UnityEvents](https://docs.unity3d.com/Manual/UnityEvents.html), like the example on the [Getting Started](../../overview/getting-started.md#without-code) page, you can assign methods like `SetVolume()`, or `Stop()` when a certain event is triggered.

#### Play by script (API)

When you call `BroAudio.Play(_soundID)`, a value of type [IAudioPlayer](../../reference/api-documentation/interface/iaudioplayer.md) is returned. It represents the instance of AudioPlayer, and you can cache it to execute other APIs (like `SetVolume()`, `Stop()`...etc) when needed.&#x20;

While the above approaches help you control the AudioPlayer easily, there's one thing to be aware of: Object Pooling.

## Object Pool

Object pool is a common performance optimization strategy in software engineering, especially in game development, it minimizes the CPU cost of frequent creation and memory usage by reusing objects in a limited pool. Using object pooling with the AudioPlayer is necessary due to the frequent changes in the amount of sound in games. **In BroAudio, once the audio finishes playing, it is recycled into the pool, waiting to be reused.**

### Accessing a recycled AudioPlayer?

Under the object pooling design, a drawback is that if you cache the reference of AudioPlayer (via IAudioPlayer or any other type), you might access an AudioPlayer that has finished playing and has been recycled or even reused by other sounds. <mark style="color:green;">Don't worry, Bro has got your back!</mark> This issue has been resolved since [ver 1.04](../../others/release-notes.md#version-1.04-unity-asset-store). If you try to access a recycled AudioPlayer, instead of encountering errors or getting a wrong player, the execution will be canceled, and a warning will be logged to the console. You can also set whether to log this warning in _<mark style="color:orange;">Tools > BroAudio > Preferences</mark>_.

## Play Audio Across Scenes

When the first scene is loaded, BroAudio will automatically create a singleton sound manager object and set it to [DontDestoryOnLoad](https://docs.unity3d.com/ScriptReference/Object.DontDestroyOnLoad.html). The sound manager will be the parent of all AudioPlayers, and since it is not destroyed when loading a new scene, the AudioPlayers persist as well. Therefore, all audio will not be interrupted and can continue playing across multiple scenes.

This design offers more possibilities for the game's audio management. For example, you can allow the same music to keep playing across scenes, create a smooth ambience sound transition when the scene change, or make the SFXs from the previous scene naturally fade out without a sudden stop.

{% hint style="info" %}
If there is a need to stop all sounds when the scene changes or restarts, use `BroAudio.Stop(BroAudioType.All)` [API](../../reference/api-documentation/class/broaudio.md#public-methods).
{% endhint %}
