---
cover: ../.gitbook/assets/AudioClipEditor.png
coverY: 0
layout:
  width: default
  cover:
    visible: true
    size: hero
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

# Audio Clip Editor

## Introduction

This could help you edit the audio file permanently in Unity. So you don't need to use other tools to edit the original file. Also, this can help you decrease the file size by trimming unwanted segments or converting it to mono.

## How To Use?

Select an audio clip to start editing. Click <mark style="background-color:orange;">Save</mark> to export the edited clip and replace the original file. Click <mark style="background-color:green;">Save As</mark> to export the edited clip as a new file.

## Parameters

### Playback Position (in seconds)

{% hint style="success" %}
The playback position can be controlled by dragging the icon on the waveform GUI
{% endhint %}

**Start:** The clip's beginning; segments before this will be trimmed.

**End:** Marks the clip's ending; segments after this will be trimmed.

**In:** The duration time of the fade-in process that will be applied.&#x20;

**Out:** The duration time of the fade-out process that will be applied.&#x20;

**Delay:** The duration time before the clip's beginning, silence segments will be added.

### Volume

Select the tab to change the volume when exporting the clip.

{% hint style="warning" %}
Increasing the volume too much may cause clipping (distortion). Gradually increase the volume, saving frequently to listen and observe the waveform changes as you go.
{% endhint %}

### Reverse

Reversing the entire clip, so it plays backward, can create a cool effect.

### Convert To Mono

Converting a stereo sound to mono can decrease the file size a lot! It can help you save some memory at runtime, and decrease the size of your final build.

There are two methods available here

1. **Downmixing:** Mix the two channels together.
2. **Left/ Right:** Choose one of the channels, and abandon the other one.
