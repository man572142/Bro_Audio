---
description: The interface of the Music Player feature
---

# IMusicPlayer

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Description

This is returned by the [AsBGM()](../../../core-features/audio-player/music-player.md#asbgm) method. It's basically the same as [IAudioPlayer](iaudioplayer.md), but it includes a SetTransition() method and excludes the AsBGM() method.

## Public Methods

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="129">Return</th><th width="193">Parameters</th><th width="249">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>SetTransition</strong></mark></td><td><a href="imusicplayer.md">IMusicPlayer</a></td><td><a href="../enums/transition.md"><mark style="color:green;">Transition</mark> </a>transition</td><td>Set the transition mode that the music player will apply when playing the next BGM</td></tr><tr><td></td><td></td><td><a href="../enums/transition.md"><mark style="color:green;">Transition</mark> </a>transition, <mark style="color:green;">float</mark> fadeTime</td><td>Same as above, but can override the fade time set in LibarayManager</td></tr><tr><td></td><td></td><td><a href="../enums/transition.md"><mark style="color:green;">Transition</mark> </a>transition, <a href="../enums/stopmode.md"><mark style="color:green;">StopMode</mark> </a>stopMode</td><td>Same as above, but can specify the StopMode</td></tr><tr><td></td><td></td><td><a href="../enums/transition.md"><mark style="color:green;">Transition</mark> </a>transition, <a href="../enums/stopmode.md"><mark style="color:green;">StopMode</mark> </a>stopMode, <mark style="color:green;">float</mark> fadeTime</td><td>Same as above with all the parameters</td></tr></tbody></table>
