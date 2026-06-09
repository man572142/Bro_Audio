---
cover: >-
  https://images.unsplash.com/photo-1529982840618-3ec6ead42f33?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHwxfHxpbXBhY3R8ZW58MHx8fHwxNzI3NzYwNzUyfDA&ixlib=rb-4.0.3&q=85
coverY: 748
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

# 💪 Velocity

## Introduction

Velocity helps determine which clip should be played in a given scenario. The concept originates from the MIDI keyboards in music production world. These devices capture the [velocity ](https://youtu.be/EjCPiVFwZfQ?si=N5moA_od9JvAOCz-)(the force with which a note is played) and use it to trigger different audio samples based on this value. This feature was designed in a similar way.

## How To Use?

Set the [PlayMode](./#playmode) to **Velocity** and set the value for each clip from low to high. These values will then be used as the bottom of a certain velocity range. Say if you have 2 clips set as 0 and 50, when the given velocity is 30, the first clip will be played, and if the given velocity is 60, the second clip will be played.

To give a velocity, simplicity just add [.SetVelocity(velocity)](../../../reference/api-documentation/interface/iaudioplayer.md#public-methods) to the [method chain](../../../reference/api-documentation/#the-method-chaining-design).

```csharp
BroAudio.Play(_hit).SetVelocity(60);
```

{% embed url="https://youtu.be/MnK2cHQMA64?si=SABBsPxwuB-via_z" %}
