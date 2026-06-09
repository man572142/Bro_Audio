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

# Useful Attrubutes

### \[Volume]

Mark a float field to be shown as a [BroAuido volume slider](../core-features/library-manager/design-the-sound/volume-control.md#editor-slider) ranging from 0 to 10 (-80dB to +20dB).

{% hint style="warning" %}
The volume range in WebGL is from 0 to 1, and the slider will be linear.
{% endhint %}

### \[Volume(bool canBoost)]

If canBoost is true, the float field will be shown as a [BroAuido volume slider](../core-features/library-manager/design-the-sound/volume-control.md#editor-slider) with a range from 0 to 10. otherwise, it will be shown as a regular linear slider.

{% hint style="warning" %}
canBoost = true in WebGL is not supported.
{% endhint %}

### \[Frequency]

Mark a float field to be shown as a Logarithmic slider ranging from 10Hz to 22000Hz.

### \[Pitch]

Mark a float field to be shown as a linear slider ranging from -3 to 3
