---
cover: >-
  https://images.unsplash.com/photo-1514241516423-6c0a5e031aa2?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHwyfHxzdW5yaXNlfGVufDB8fHx8MTcwNDI3ODEyN3ww&ixlib=rb-4.0.3&q=85
coverY: -5.960294117647059
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

# 🎚️ Fading

## Introduction

### Fade In/Out

Fade in and fade out are techniques that make the playback experience smoother and more fluid. In addition to setting these fading for each AudioClip in LibraryManager, you also have APIs like `SetVolume()` and `SetEffect()`that offer a fadeTime(float value) overload, allowing all changes to appear more natural.

### Cross Fade

Crossfading is the technique of smoothly transitioning from one sound to another by gradually decreasing the volume of the first sound while simultaneously increasing the volume of the second.

## All fadings are framerate-independent in BroAudio

One of the main reasons BroAudio uses volume control through AudioMixer instead of AudioSource is that AudioMixer is frame rate independent, allowing volume changes to occur more naturally and precisely.

If you're interested, there's an excellent [article](https://johnleonardfrench.com/how-to-fade-audio-in-unity-i-tested-every-method-this-ones-the-best/#first_method) that explains the differences between the two.

{% hint style="warning" %}
Fading in WebGL will still be framerate-dependent. [more details](../../../overview/compatibility.md#webgl)
{% endhint %}

