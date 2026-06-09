---
description: Parameters for setting effects.
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

# Effect

| NameSpace    | Accessibility |
| ------------ | ------------- |
| Ami.BroAudio | public        |

{% hint style="warning" %}
The constructor of this struct is **internal**. Please use the static factory method for creation. This design provides a more organized and consistent parameter configuration and method names with clearer meanings.
{% endhint %}

## Public Static Factory Method

<table><thead><tr><th width="163">Method</th><th>Parameters</th><th>Description</th></tr></thead><tbody><tr><td><mark style="color:orange;"><strong>HighPass</strong></mark></td><td><mark style="color:green;">float</mark> frequency, <mark style="color:green;">float</mark> fadeTime, <a href="../enums/ease.md"><mark style="color:green;">Ease</mark></a> ease = <a href="../class/broadvice.md">BroAdvice</a>.HighPassInEase</td><td>Create a <a href="../../audio-terminology.md#high-pass-filter">High Pass Filter</a> effect and set its frequency at the given value.<br><br><strong>fadeTime:</strong> The time duration from 20Hz to the given frequency.<br><br><strong>ease(optional):</strong> the ease function that applies while fading the frequency</td></tr><tr><td><mark style="color:orange;"><strong>ResetHighPass</strong></mark></td><td><mark style="color:green;">float</mark> fadeTime, <a href="../enums/ease.md"><mark style="color:green;">Ease</mark></a> ease = <a href="../class/broadvice.md">BroAdvice</a>.HighPassOutEase</td><td>For resetting the <a href="../../audio-terminology.md#high-pass-filter">High Pass Filter</a> effect (back to 20Hz).<br><br><strong>fadeTime:</strong> The time duration from the current frequency to 20Hz.</td></tr><tr><td><mark style="color:orange;"><strong>LowPass</strong></mark></td><td><mark style="color:green;">float</mark> frequency, <mark style="color:green;">float</mark> fadeTime, <a href="../enums/ease.md"><mark style="color:green;">Ease</mark></a> ease = <a href="../class/broadvice.md">BroAdvice</a>.LowPassInEase</td><td>Create a <a href="../../audio-terminology.md#low-pass-filter">Low Pass Filter</a> effect and set its frequency at the given value.<br><br><strong>fadeTime:</strong> The time duration from 22000Hz to the given frequency.<br><br><strong>ease(optional):</strong> the ease function that applies while fading the frequency</td></tr><tr><td><mark style="color:orange;"><strong>ResetLowPass</strong></mark></td><td><mark style="color:green;">float</mark> frequency, <mark style="color:green;">float</mark> fadeTime, <a href="../enums/ease.md"><mark style="color:green;">Ease</mark></a> ease = <a href="../class/broadvice.md">BroAdvice</a>.LowPassOutEase</td><td>For resetting the <a href="../../audio-terminology.md#low-pass-filter">Low Pass Filter</a> effect (back to 22000Hz).<br><br><strong>fadeTime:</strong> The time duration from the current frequency to 22000Hz.</td></tr><tr><td><mark style="color:orange;"><strong>Custom</strong></mark></td><td><mark style="color:green;">string</mark> exposedParameterName, <mark style="color:green;">float</mark> value, <mark style="color:green;">float</mark> fadeTime, <a href="../enums/ease.md"><mark style="color:green;">Ease</mark></a> ease = <a href="../enums/ease.md">Ease</a>.Linear</td><td>Create a custom effect. This is similar to using the <a href="https://docs.unity3d.com/ScriptReference/Audio.AudioMixer.SetFloat.html">UnityAPI AudioMixer.SetFloat()</a>, which requires adding an effect and exposing parameters in the AudioMixer. The only difference is that the effect referred to can only be applied to the 'Effect' track of BroAudioMixer, which can be set via <a href="../../../tools/audio-effect-editor.md">AudioEffectEditor</a>.</td></tr></tbody></table>

