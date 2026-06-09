---
description: The class of the SoundSource component
---

# SoundSource

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Public Variables

<table><thead><tr><th width="181">Variable</th><th width="196">Type</th><th>Description</th></tr></thead><tbody><tr><td><mark style="color:purple;"><strong>CurrentPlayer</strong></mark></td><td><a href="../interface/iaudioplayer.md">IAudioPlyer</a></td><td>The currently playing player</td></tr><tr><td><mark style="color:purple;"><strong>IsPlaying</strong></mark></td><td>bool</td><td>Whether the CurrentPlayer is playing</td></tr></tbody></table>

## Public Methods

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="119">Return</th><th width="242">Parameters</th><th width="212">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>Play</strong></mark></td><td>void</td><td><a href="../struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td><strong>Plays the audio base on the current PositionMode</strong></td></tr><tr><td></td><td>void</td><td><a href="../struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id, <mark style="color:green;">Vector3</mark> position</td><td>Plays audio in 3D space at the given position<br>(SpatialBlend will be set to 3D automatically)</td></tr><tr><td></td><td>void</td><td><a href="../struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id, <mark style="color:green;">Transform</mark> followTarget</td><td>Plays audio in 3D space and keeps it following the target continuously<br>(SpatialBlend will be set to 3D automatically)</td></tr><tr><td><mark style="color:orange;"><strong>PlayGlobally</strong></mark></td><td>void</td><td><a href="../struct/audioid.md"><mark style="color:green;">SoundID</mark></a> id</td><td>Plays audio globally (2D)</td></tr><tr><td><mark style="color:orange;"><strong>Stop</strong></mark></td><td>void</td><td></td><td>Stop playing audio</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> fadeOut</td><td>Stop playing audio by the given fadeOut time</td></tr><tr><td><mark style="color:orange;"><strong>SetVolume</strong></mark></td><td>void</td><td><mark style="color:green;">float</mark> volume</td><td>Immediately sets the volume of the currently playing player</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> volume, <mark style="color:green;">float</mark> fadeTime</td><td>Adjusts the volume of the currently playing player over the specified fade duration</td></tr><tr><td><mark style="color:orange;"><strong>SetPitch</strong></mark></td><td>void</td><td><mark style="color:green;">float</mark> pitch</td><td>Immediately sets the pitch of the currently playing player</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> pitch, <a href="../enums/broaudiotype.md"><mark style="color:green;">BroAudioType</mark></a> audioType</td><td>Adjusts the pitch of the currently playing player over the specified fade duration</td></tr></tbody></table>
