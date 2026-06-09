---
cover: >-
  https://images.unsplash.com/photo-1702360373665-af7bf9766ab4?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHJhbmRvbXx8fHx8fHx8fDE3MDQyNzc4MjR8&ixlib=rb-4.0.3&q=85
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

# 🔁 Looping

## Introduction <a href="#introduction" id="introduction"></a>

Looping allows an [AudioClip](https://docs.unity3d.com/Manual/class-AudioClip.html) to be continuously replayed. It's a common technique used for playing sounds such as **Music** and **Ambience** in games. However, if a sound is not specifically designed for looping, its beginning and end might not match seamlessly. Direct replay would result in a noticeable change.

This is where **SeamlessLoop** comes in. It enables an [AudioClip](https://docs.unity3d.com/Manual/class-AudioClip.html) to start playing again on a new audio track just before the current one finishes. By gradually [cross-fading](fade-in-out-and-cross-fade.md#crossfade) between the two tracks, it creates an illusion that the sound never stops at all.

## **How To Use?**

You can find the **Looping** option in the 'Overall' tab of an AudioEntity. If one of the looping options is selected, the AudioEntity will play accordingly.\
[<mark style="color:blue;">Didn't see the option?</mark>](#user-content-fn-1)[^1]

<figure><img src="../../../.gitbook/assets/SeamlessLoop.png" alt=""><figcaption></figcaption></figure>

### Loop

The sound will replay when it's finished.

### Seamless Loop

The sound will replay **before** it ends.

If this option is selected, "Seamless Setting" will appear, allowing you to set the transition timing.

1. **Clip Setting:** The replay begins as the clip starts fading out.
2. **Time:** The replay starts a specified number of seconds before the end.
3. **Tempo:** The replay starts a specified number of beats before the end.

### Change Clip Per Loop

When an AudioEntity has more than one clip and either **Loop** or **Seamless Loop** is enabled, a **Change Clip Per Loop** toggle appears below the looping options.

Enable it to pick a new clip on every loop iteration, following the entity's [Play Mode](./#playmode) (e.g. Random or Shuffle), instead of repeating the same clip. This is handy for varied ambiences or evolving loops without authoring a single long clip.

{% hint style="info" %}
Volume changes made through `IAudioPlayer.SetVolume()` are carried across each loop, so fades stay smooth even when the loop transitions between clips or audio tracks.
{% endhint %}

[^1]: If you can't see this option, it might be because the GUI setting of this AudioType is set to be invisible. [Click here for more details.](../../customization.md#displayed-properties)
