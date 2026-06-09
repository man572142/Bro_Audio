---
description: The interface of Dominator Player feature
---

# IPlayerEffect

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Description

This is returned by the [AsDominator()](../../../core-features/audio-player/dominator-player.md#asdominator) method. It's basically the same as [IAudioPlayer](iaudioplayer.md), but it includes 3 new methods and excludes the AsDominator() method.

## Public Methods

<table data-full-width="false"><thead><tr><th width="170">Method</th><th width="129">Return</th><th width="213">Parameters</th><th width="249">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>QuietOthers</strong></mark></td><td><a href="iplayereffect.md">IPlayerEffect</a></td><td><mark style="color:green;">float</mark> othersVolume, <mark style="color:green;">float</mark> fadeTime = <a href="../class/broadvice.md">0.5f</a></td><td>While this audio player is playing, the volume of other audio players will be lowered to the given volume</td></tr><tr><td></td><td></td><td><mark style="color:green;">float</mark> othersVolume, <mark style="color:green;">Fading</mark> fading</td><td>Same as above, but can provide a custom fading setting</td></tr><tr><td><mark style="color:orange;"><strong>LowPassOthers</strong></mark></td><td><a href="iplayereffect.md">IPlayerEffect</a></td><td><mark style="color:green;">float</mark> frequency = <a href="../class/broadvice.md">300f</a>, <mark style="color:green;">float</mark> fadeTime = <a href="../class/broadvice.md">0.5f</a></td><td>While this audio player is playing, a lowpass filter will be added to other audio players (i.e. their higher frequencies will be cutted off)</td></tr><tr><td></td><td></td><td><mark style="color:green;">float</mark> frequency, <mark style="color:green;">Fading</mark> fading</td><td>Same as above, but can provide a custom fading setting</td></tr><tr><td><mark style="color:orange;"><strong>HighPassOthers</strong></mark></td><td><a href="iplayereffect.md">IPlayerEffect</a></td><td><mark style="color:green;">float</mark> frequency = <a href="../class/broadvice.md">2000f</a>, <mark style="color:green;">float</mark> fadeTime = <a href="../class/broadvice.md">0.5f</a></td><td>While this audio player is playing, a highpass filter will be added to other audio players (i.e. their lower frequencies will be cutted off)</td></tr><tr><td></td><td></td><td><mark style="color:green;">float</mark> frequency, <mark style="color:green;">Fading</mark> fading</td><td>Same as above, but can provide a custom fading setting</td></tr></tbody></table>
