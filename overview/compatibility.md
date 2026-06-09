---
layout:
  width: default
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

# Compatibility

## Unity Version

BroAudio works on Unity 2020.3+, including 2020.3, 2020.3, 2021, 2022, 2023 and Unity 6

## Platform

| Platform                               | Tested | Limitation                          |
| -------------------------------------- | ------ | ----------------------------------- |
| PC                                     | ✅      | ❌                                   |
| Mac                                    | ✅      | ❌                                   |
| Android                                | ✅      | ❌                                   |
| iOS                                    | ✅      | ❌                                   |
| WebGL                                  | ✅      | [see below](compatibility.md#webgl) |
| Console (Playstation, Xbox, Switch...) | ❌      | unknown                             |

### WebGL

Audio in WebGL is heavily limited in Unity, since BroAudio is an add-on plugin based on Unity Audio, many features are limited as well.&#x20;

List of the BroAudio features that are **not supported** in WebGL.

* [Dominator Player](../core-features/audio-player/dominator-player.md)
* [AudioEffect](../core-features/audio-effect.md)

List of the BroAudio features that are **limited** in WebGL.

* [Volume](../core-features/library-manager/design-the-sound/volume-control.md) range will be limited to 0\~1 (-80dB \~ 0dB)
* [Fading](../core-features/library-manager/design-the-sound/fade-in-out-and-cross-fade.md) will **not** be frame rate independent

{% hint style="info" %}
Most of the listed feature limitations are due to the lack of support for AudioMixer, which is only mentioned on the [AudioMixer](https://docs.unity3d.com/Manual/AudioMixer.html) page.
{% endhint %}

For more information about Audio in WebGL, see [Unity Docs](https://docs.unity3d.com/Manual/webgl-audio.html)
