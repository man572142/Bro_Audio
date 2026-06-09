---
cover: >-
  https://images.unsplash.com/photo-1516865674991-9bb4878e3476?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHwxfHx2b2x1bWV8ZW58MHx8fHwxNzA0Mjc3ODc2fDA&ixlib=rb-4.0.3&q=85
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

# 🔊 Volume

## Volume Unit

We often encounter two types of volume units in Unity and BroAudio, both relative to the original audio file's volume:

1. **Normalized Volume:** When set to 1, it represents 100% of the file's original volume, while 0 represents 0% volume, which means complete silence. Unity's [AudioSource](https://docs.unity3d.com/Manual/class-AudioSource.html) volume uses this unit.
2. **Decibel Volume (dB):** This is the most commonly used unit to describe volume. It represents a logarithmic value. When set to 0dB, it means no changes to the file's original volume (so 0dB = 1 normalized volume). Unity's [AudioMixer](https://docs.unity3d.com/Manual/AudioMixer.html) uses this unit. [Decibel(dB) Wikipedia](https://en.wikipedia.org/wiki/Decibel)

{% hint style="info" %}
Decibel (dB) is a relative unit, so different reference objects and ranges can lead to different calculations. However, here we are referring to **dBFS**, which uses the original volume of the audio file as its Full Scale.
{% endhint %}

These two volume units can be converted into each other. Although **you don't need to do the calculations** yourself, if you are interested, the formula is as follows:

$$dB = 20log (V1/V2)$$

(V1/V2) is the same as Normalized Volume. There are many simple [online calculators](https://daycounter.com/Calculators/Decibels-Calculator.phtml) available for use. By entering the Normalized Volume into the Voltage Gain, you can convert it to dB.

## Volume Range

In BroAudio, every sound plays through the [Audio Mixer](../../../designs/audio-mixer.md), with a volume range of 0 to 10 (-80dB to +20dB). This is **20dB more** than what [Audio Source](https://docs.unity3d.com/Manual/class-AudioSource.html) offers, meaning you can produce sounds louder than the original files. This feature helps you to easily adjust and balance the overall volume.

{% hint style="warning" %}
Volume range will be limited to 0\~1 (-80dB\~0dB) in WebGL, [more details.](../../../overview/compatibility.md#webgl)
{% endhint %}

## Editor Slider

The volume slider is based on decibel value; it's a logarithmic slider instead of a linear slider like [Audio Source](https://docs.unity3d.com/Manual/class-AudioSource.html)'s volume. This matches human sound perception better and allows for easier fine-tuning.

To further ensure precision, most audio software, including [Unity Audio Mixer](https://docs.unity3d.com/Manual/AudioMixer.html), features custom scale settings. Below is a comparison of different slider types, with each measured in decibels(dB) :

<figure><img src="../../../.gitbook/assets/Slider difference.PNG" alt=""><figcaption><p>-6dB ≒ 0.5 volume (half volume), -20dB ≒ 0.1 volume, <a href="https://en.wikipedia.org/wiki/Decibel">Decibel(dB) Wikipedia</a></p></figcaption></figure>

## All volume settings work in conjunction

There are four volume settings available in BroAudio, and they all work in conjunction.

1. [Clip Volume](./#clip-parameters)&#x20;
2. [Master Volume](./#master-volume) (of the entity)
3. [BroAudio.SetVolume(SoundID)](../../../reference/api-documentation/class/broaudio.md#operations) or [IAudioPlayer.SetVolume()](../../../reference/api-documentation/interface/iaudioplayer.md#public-methods)
4. [BroAudio.SetVolume(BroAudioType)](../../../reference/api-documentation/class/broaudio.md#operations)

The final playback volume (in normalized) will be

<mark style="background-color:green;">**Clip Volume**</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">\*</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">**Entity Master Volume**</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">\*</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">**TrackVolume**</mark><mark style="background-color:green;">(if 3. was used) \*</mark> <mark style="background-color:green;"></mark><mark style="background-color:green;">**AudioTypeVolume**</mark><mark style="background-color:green;">(if 4. was used)</mark>
