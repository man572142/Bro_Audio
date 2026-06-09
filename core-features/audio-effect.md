---
cover: >-
  https://images.unsplash.com/photo-1608311821539-57a58f13b074?crop=entropy&cs=srgb&fm=jpg&ixid=M3wxOTcwMjR8MHwxfHNlYXJjaHw0fHxlZmZlY3R8ZW58MHx8fHwxNzEzNzcwMzY2fDA&ixlib=rb-4.0.3&q=85
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

# Audio Effect

## Introduction

Audio effects can enhance the immersion and fun of your game. BroAudio supports all of Unity's built-in audio effects and allows for dynamic parameter adjustments during gameplay.

There are two ways to apply audio effects:&#x20;

* `BroAudio.SetEffect(BroAudioType)`&#x20;
* `IAudioPlayer.AddXXXEffect()`&#x20;

## <mark style="color:$success;">Set effects</mark> for a specific type

This applies an [audio **mixer** effect](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioEffectMixer.html) to all audio of a specified [BroAudioType](../reference/api-documentation/enums/broaudiotype.md). It's ideal when you want to apply a single effect to an entire category of sounds, such as changing the sound of all ambient tracks or sound effects simultaneously.

### How To Use

#### Create an '[Effect](../reference/api-documentation/struct/effect.md)' data struct

To trigger an audio effect at runtime, you need to create an [Effect](../reference/api-documentation/struct/effect.md) struct with the parameter values via the static factory methods. This data will be passed as a parameter to the `SetEffect` method.

<pre class="language-csharp"><code class="lang-csharp"><strong>// Create high pass filter and low pass filter effect
</strong><strong>Effect highPassFilter = Effect.HighPass(highPassFrequency, fadeTime);
</strong>Effect lowPassFilter = Effect.LowPass(lowPassFrequency, fadeTime);

//Create a custom effect
Effect reverbEffect = Effect.Custom("exposedParameter", value, fadeTime);
//The custom effect and "exposedParameter" must be created and available in BroAudioMixer.
</code></pre>

### `BroAudio.SetEffect(Effect, BroAudioType)`

Common use cases and API examples:

#### When the player is underwater

```csharp
Effect lowPassFilter = Effect.LowPass(freq, fadeTime);
BroAudio.SetEffect(lowPassFilter, BroAudioType.SFX | BroAudioType.Ambience); 

// Or write it in one line!
BroAudio.SetEffect(Effect.LowPass(freq, fadeTime)); 

// If the BroAudioType is not specified, it will apply to all.
```

#### Add more effect

Bro Audio only offers two initial effects, but you can add more! However, since BroAudio has already set up a comprehensive audio mixer with numerous Exposed Parameters, editing it directly through Unity's Audio Mixer Window can be challenging.&#x20;

Therefore, it's highly recommended to use the [Audio Effect Editor](../tools/audio-effect-editor.md) to add more effect. It lets you focus on the **Effect** track and only displays the [Exposed Parameters](https://docs.unity3d.com/6000.2/Documentation/Manual/AudioMixerInspectors.html) of the Audio Effects.



## <mark style="color:orange;">Add Effects</mark> to Individual Audio Players

This applies [audio **filter** effects](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioEffect.html) to a specific audio instance, offering more granular control.

### How To Use

Use the chaining methods on the&#x20;[`IAudioPlayer`](../reference/api-documentation/interface/iaudioplayer.md) instance returned by [`BroAudio.Play()`](../reference/api-documentation/class/broaudio.md#public-methods) methods. For example:

```csharp
// Play a sound and apply a low-pass filter to it.
BroAudio.Play(sound)
    .AddLowPassEffect(lowpass => lowpass.cutoffFrequency = 1000f);
    
// You can also add multiple effects
// If the onSet parameter doesn't fulfill, the effect use the Unity's default value
BroAudio.Play(anotherSound)
        .AddLowPassEffect()
        .AddReverbEffect(reverb => reverb.reverbPreset = AudioReverbPreset.Room);
```

### Available Effects

You can chain any of the following effect methods to an [IAudioPlayer](../reference/api-documentation/interface/iaudioplayer.md):

* [AddChorusEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioChorusFilter.html)
* [AddDistortionEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioDistortionFilter.html)
* [AddEchoEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioEchoFilter.html)
* [AddHighPassEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioHighPassFilter.html)
* [AddLowPassEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioLowPassFilter.html)
* [AddReverbEffect()](https://docs.unity3d.com/6000.2/Documentation/Manual/class-AudioReverbFilter.html)

Each of these methods accepts an `onSet` action to configure the effect's parameters.

#### Removing Effects

You can also remove effects from an audio player using the corresponding `Remove...Effect()` methods:&#x20;

```csharp
var myPlayer = BroAudio.Play("mySound")
                                .AddLowPassEffect();
                                
// ... later on
myPlayer.RemoveLowPassEffect();
```

