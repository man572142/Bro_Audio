---
description: >-
  The starting point for creating and innovating, here you can add the required
  AudioClip and define its behavior and performance during playback.
cover: ../../.gitbook/assets/LibraryManager (1).png
coverY: 23
layout:
  width: default
  cover:
    visible: true
    size: hero
  title:
    visible: true
  description:
    visible: true
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

# Library Manager

The library is primarily composed of two data structures: **Asset** and **Entity**. Their relationship is illustrated in the following diagram:

<figure><img src="../../.gitbook/assets/DataStructure.png" alt=""><figcaption></figcaption></figure>

## Asset

Asset is a [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) that contains a collection of Entities. You can group commonly used sounds together in the same Asset. As you can see in the diagram above, all sounds related to the "Player" are stored within the Player Asset.

## **Entity**

An Entity represents a single sound, so when it is played, only one AudioClip from its [ClipList](design-the-sound/#clip-list) will be played. Here, we set which AudioClip it should play, how it should be played, and what kind of characteristic it will have during playback.

### SoundID

When an entity is created in the LibraryManager, a SoundID is automatically generated. Once the SoundID is serialized (by \[SerializedField] or public), a dropdown menu appears in the inspector.

[see how to do this in action](../../overview/getting-started.md#declare-an-audioid-and-use-broaudio.play-to-play-it)

