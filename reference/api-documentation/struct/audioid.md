---
description: To address Entity in the inspector
---

# SoundID

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Public Variables

<table><thead><tr><th width="181">Variable</th><th width="196">Type</th><th>Description</th></tr></thead><tbody><tr><td><del><mark style="color:purple;"><strong>ID</strong></mark></del></td><td><del><mark style="color:green;">int</mark></del></td><td>A <del>unique ID for an</del> <a href="../../../core-features/library-manager/#entity"><del>Entity</del></a><br>Deprecated since <a href="../../../others/release-notes.md#ver.-3.1.0-github-unity-asset-store">v3.1.0</a></td></tr></tbody></table>

## Extension Method

<table data-full-width="false"><thead><tr><th width="139.79998779296875">Method</th><th width="136">Return</th><th width="123">Parameters</th><th width="339">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>ToAudioType</strong></mark></td><td><a href="../enums/broaudiotype.md">BroAudioType</a></td><td></td><td>Convert ID to AudioType</td></tr><tr><td><del><mark style="color:orange;"><strong>ToName</strong></mark></del></td><td><del>string</del></td><td></td><td><p><del>Get the</del> <a href="../../../core-features/library-manager/#entity"><del>Entity</del></a> <del>name of the ID</del></p><p> Use ToString() instead</p></td></tr><tr><td><mark style="color:orange;"><strong>IsValid</strong></mark></td><td>bool</td><td></td><td>Checks if this ID is available at runtime</td></tr><tr><td><mark style="color:orange;"><strong>GetAudioClip</strong></mark></td><td><a href="https://docs.unity3d.com/ScriptReference/AudioClip.html">AudioClip</a></td><td></td><td>Get the AudioClip based on the <a href="../../../core-features/library-manager/design-the-sound/#playmode">PlayMode</a> setting of the <a href="../../../core-features/library-manager/#entity">Entity</a></td></tr><tr><td></td><td><a href="https://docs.unity3d.com/ScriptReference/AudioClip.html">AudioClip</a></td><td><mark style="color:green;">int</mark> velocity</td><td>Get the AudioClip based on the <a href="../../../core-features/library-manager/design-the-sound/velocity.md">Velocity </a>setting of the <a href="../../../core-features/library-manager/#entity">Entity</a></td></tr><tr><td><mark style="color:orange;"><strong>Play</strong></mark></td><td><a href="../interface/iaudioplayer.md">IAudioPlyer</a></td><td></td><td>Same as <a href="../class/broaudio.md#public-methods">BroAudio.Play()</a></td></tr><tr><td></td><td><a href="../interface/iaudioplayer.md">IAudioPlyer</a></td><td><mark style="color:green;">Vector3</mark> position</td><td>Same as <a href="../class/broaudio.md#public-methods">BroAudio.Play()</a></td></tr><tr><td></td><td><a href="../interface/iaudioplayer.md">IAudioPlyer</a></td><td><mark style="color:green;">Transform</mark> followTarget</td><td>Same as <a href="../class/broaudio.md#public-methods">BroAudio.Play()</a></td></tr><tr><td><mark style="color:orange;"><strong>HasAnyPlayingInstances</strong></mark></td><td>bool</td><td></td><td>Check if a sound is currently playing anywhere</td></tr></tbody></table>
