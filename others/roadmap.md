---
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

# Roadmap

{% tabs %}
{% tab title="In Progress" %}
**UPM support**
{% endtab %}

{% tab title="Planned" %}
#### Improved Audio Effect workflow

When using the `BroAudio.SetEffect()` API. We can use LowPass and HighPass effects without passing the "exposed parameter" string value; instead, we can simply pass the desired value. The Effect.Custom() method should operate in a similar manner.

#### Allowing AudioEffect to Operate on Multiple Sounds Individually

Currently, BroAudio only provides one Effect track, so it's not possible to add different Effects to various sounds individually.
{% endtab %}

{% tab title="Under Consideration" %}
#### Bro Audio Dynamic Reverb Solution

#### Audio Occlusion

#### Decibel Base Animation Curve View

#### Adaptive Music
{% endtab %}
{% endtabs %}



