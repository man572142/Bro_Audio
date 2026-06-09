---
description: The interface of the AudioPlayer, it's the main entry of the Method Chaining
layout:
  width: default
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

# IAudioPlayer

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

## Description

When [BroAudio.Play(id)](../class/broaudio.md#public-methods) is called, it returns an IAudioPlayer representing the AudioPlayer currently playing the audio. You can chain more methods after this, or cache the instance for later use.

This interface is composed of several interfaces, including [IAudioStoppable](iaudioplayer.md#iaudiostoppable),  [IVolumeSettable](iaudioplayer.md#ivolumesettable), [IEffectDecoratable](iaudioplayer.md#ieffectdecoratable), [IMusicDecoratable](iaudioplayer.md#imusicdecoratable).

{% hint style="warning" %}
The AudioPlayer has utilized the [ObjectPool design](../../../core-features/audio-player/#object-pool), which will recycle the AudioPlayer to the pool once it has finished its playing (or being stopped manually).

If you've cached the instance of the AudioPlayer and tried to access it after it finished (recycled). To avoid accessing the wrong player, Bro will reject your access and log a warning to indicate the target player is unavailable.
{% endhint %}

## Properties

| Property                                                                                                                                                                                                                              | Type                                                      | Description                                                    |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------- | -------------------------------------------------------------- |
| <mark style="color:purple;">**ID**</mark>                                                                                                                                                                                             | SoundID, int                                              | The [SoundID](../struct/audioid.md) that the player is playing |
| <mark style="color:purple;">**IsPlaying**</mark>                                                                                                                                                                                      | bool                                                      | Return true if the player's AudioSource is playing             |
| <mark style="color:purple;">**IsActive**</mark>                                                                                                                                                                                       | bool                                                      | Returns true if the player is about to play or is playing      |
| <mark style="color:purple;">**AudioSource**</mark>                                                                                                                                                                                    | [IAudioSourceProxy](iaudiosourceproxy.md)                 | Gets the AudioSource component of the player                   |
| <mark style="color:purple;">**CurrentPlayingClip**</mark>                                                                                                                                                                             | IBroAudioClip                                             | Gets the clip that the player is playing                       |
| ~~<mark style="color:purple;">**OnEndPlaying**</mark>~~ <mark style="color:red;">**deprecated \<use**</mark> [<mark style="color:red;">**OnEnd**</mark>](iaudioplayer.md#event-messages) <mark style="color:red;">**instead>**</mark> | ~~event Action<~~[~~SoundID~~](../struct/audioid.md)~~>~~ | ~~Triggered when the audio player has finished playing~~       |

## Public Methods&#x20;

<table data-full-width="false"><thead><tr><th width="168">Method</th><th width="129">Return</th><th width="183">Parameters</th><th width="265">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>GetOutputData</strong></mark></td><td>void</td><td><mark style="color:green;">float</mark>[] samples, <mark style="color:green;">int</mark> channels</td><td>Similar to <a href="https://docs.unity3d.com/ScriptReference/AudioSource.GetOutputData.html">AudioSource.GetOutputData</a>, It's recommended to use this in <a href="iaudioplayer.md#event-messages"><mark style="color:orange;">OnUpdate</mark></a></td></tr><tr><td><mark style="color:orange;"><strong>GetSpectrumData</strong></mark></td><td>void</td><td><mark style="color:green;">float</mark>[] samples, <mark style="color:green;">int</mark> channels, <a href="https://docs.unity3d.com/ScriptReference/FFTWindow.html">FFTWindow </a>window</td><td>Similar to<a href="https://docs.unity3d.com/ScriptReference/AudioSource.GetSpectrumData.html">AudioSource.GetSpectrumData</a>, It's recommended to use this in <a href="iaudioplayer.md#event-messages"><mark style="color:orange;">OnUpdate</mark></a></td></tr><tr><td><mark style="color:orange;"><strong>SetPitch</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">float</mark> pitch</td><td>Set the player's pitch.</td></tr><tr><td></td><td>IAudioPlayer</td><td><mark style="color:green;">float</mark> pitch, <mark style="color:green;">float</mark> fadeTime</td><td>Set the player's pitch by the given fade time</td></tr><tr><td><mark style="color:orange;"><strong>SetVelocity</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">int</mark> velocity</td><td>Set the velocity to determine which audio clip to play</td></tr><tr><td><mark style="color:orange;"><strong>SetFadeInEase</strong></mark></td><td>IAudioPlayer</td><td><a href="../enums/ease.md">Ease</a> ease</td><td>Sets the fade in easing function for this player</td></tr><tr><td><mark style="color:orange;"><strong>SetFadeOutEase</strong></mark></td><td>IAudioPlayer</td><td><a href="../enums/ease.md">Ease</a> ease</td><td>Sets the fade out easing function for this player</td></tr></tbody></table>

#### Event Messages

<table data-full-width="false"><thead><tr><th width="168">Method</th><th width="129">Return</th><th width="202">Parameters</th><th width="250">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>OnStart</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">Action&#x3C;IAudioPlayer></mark> </td><td>Triggered when the AudioPlayer starts playing</td></tr><tr><td><mark style="color:orange;"><strong>OnUpdate</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">Action&#x3C;IAudioPlayer></mark> </td><td>Triggered each frame while the AudioPlayer is playing</td></tr><tr><td><mark style="color:orange;"><strong>OnEnd</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">Action&#x3C;IAudioPlayer></mark> </td><td>Triggered when the AudioPlayer stops playing</td></tr><tr><td><mark style="color:orange;"><strong>OnPause</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">Action&#x3C;IAudioPlayer></mark> </td><td>Triggered when the AudioPlayer is paused</td></tr><tr><td><mark style="color:orange;"><strong>OnAudioFilterRead</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">Action</mark>&#x3C;<mark style="color:green;">float</mark>[], <mark style="color:green;">int</mark>></td><td>Similar to <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.html">MonoBehaviour</a>.OnAudioFilterRead().</td></tr></tbody></table>

### IAudioStoppable

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="127">Return</th><th width="198">Parameters</th><th width="251">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>Stop</strong></mark></td><td>void</td><td>none</td><td>Stop playing the audio</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">Action</mark> onFinished</td><td>Stop playing and trigger the action when it's finished</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> fadeOut</td><td>Stop playing the audio by the given fadeOut time</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">Action</mark> onFinished, <mark style="color:green;">float</mark> fadeOut</td><td>Stop playing the audio by the given fadeOut time, and trigger the action when it's finished</td></tr><tr><td><mark style="color:orange;"><strong>Pause</strong></mark></td><td>void</td><td>none</td><td>Pause the audio</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> fadeOut</td><td>Pause by the given fadeOut time</td></tr><tr><td><mark style="color:orange;"><strong>UnPause</strong></mark></td><td>void</td><td>none</td><td>Resume the paused audio</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> fadeIn</td><td>Resume the paused audio by the given fadeIn time</td></tr></tbody></table>

### IVolumeSettable

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="129">Return</th><th width="193">Parameters</th><th width="249">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>SetVolume</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">float</mark> volume</td><td>Set the player's volume<br>(acceptable range 0~10)</td></tr><tr><td></td><td>void</td><td><mark style="color:green;">float</mark> volume, <mark style="color:green;">float</mark> fadeTime</td><td>Set the player's volume by the given fadeTime</td></tr><tr><td><mark style="color:orange;"><strong>GetVolume</strong></mark></td><td>float</td><td>none</td><td>Get the player's current volume</td></tr></tbody></table>

### IMusicDecoratable

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="129">Return</th><th width="193">Parameters</th><th width="249">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>AsBGM</strong></mark></td><td><a href="imusicplayer.md">IMusicPlayer</a></td><td>none</td><td>Set the player as a music player, which will transition automatically if another BGM is played after it</td></tr></tbody></table>

### IEffectDecoratable

<table data-full-width="false"><thead><tr><th width="173">Method</th><th width="129">Return</th><th width="193">Parameters</th><th width="249">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>AsDominator</strong></mark></td><td><a href="iplayereffect.md">IPlayerEffect</a></td><td>none</td><td>Set the player as a dominator player, which will affect or change the behavior of other audio players during its playback</td></tr></tbody></table>

### ISchedulable

<table data-full-width="false"><thead><tr><th width="191">Method</th><th width="129">Return</th><th width="147">Parameters</th><th width="282">Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>SetScheduledStartTime</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">double</mark> dspTime</td><td>Schedules the sound to start playing at a specific time on the absolute timeline that <a href="https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSettings-dspTime.html">AudioSettings.dspTime</a> reads from .</td></tr><tr><td><mark style="color:orange;"><strong>SetScheduledEndTime</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">double</mark> dspTime</td><td>Changes the time at which a sound that has already been scheduled to play will end.</td></tr><tr><td><mark style="color:orange;"><strong>SetDelay</strong></mark></td><td>IAudioPlayer</td><td><mark style="color:green;">float</mark> time</td><td>Delays the playback start time by the specified duration in seconds.</td></tr></tbody></table>
